using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace tikthumb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly string _tempPath;
    private readonly string _outputPath;
    private readonly string _currentTimeStamp;
    private readonly UploadContext _uploadContext;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _tempPath = configuration["SaveLocation"];
        _outputPath = _tempPath + "/output";
        _currentTimeStamp = GetCurrentTimeStamp();
    }

    [AcceptVerbs("GET")]
    [ActionName("Index")]
    public IActionResult Index()
    {
        return View();
    }

    [AcceptVerbs("POST")]
    [ActionName("Index")]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> Post()
    {
        var files = Request.Form.Files;

        var uploadedVideoFile = files["videoFile"];
        var uploadedThumbnailFile = files["thumbnailFile"];

        var uploadContext = new UploadContext(_tempPath,
            _currentTimeStamp,
            uploadedVideoFile!.FileName,
            uploadedThumbnailFile!.FileName);

        await SaveFormFile(files["videoFile"]!, uploadContext.VideoSaveInfo);

        var videoSize = await GetVideoSize(uploadContext.VideoSaveInfo.SavedFileNameWithExtension);

        await ResizeAndSaveThumbnail(uploadContext.ImageSaveInfo, uploadedThumbnailFile, videoSize);

        var tempDataContext = new FfmpegContext(_tempPath, _outputPath, uploadContext, _currentTimeStamp);

        await InsertFrame(uploadContext, tempDataContext);

        return View();
    }

    private async Task<Size> GetVideoSize(string videoFileName)
    {
        var argument = $"-v error -select_streams v:0 -show_entries stream=width,height -of csv=p=0 {videoFileName}";
        var process = CreateFfmpegProcess(argument);
        process.Start();

        var stdOutput = await process.StandardOutput.ReadToEndAsync();
        _logger.LogInformation($"Stdoutput is: {stdOutput}");

        var dimensionArray = stdOutput.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x)).ToList();
        return new Size(dimensionArray[0], dimensionArray[1]);
    }

    private async Task ResizeAndSaveThumbnail(FileSaveInfo imageSaveInfo, IFormFile file, Size desiredSize)
    {
        using var stream = file.OpenReadStream();

        var image = await Image.LoadAsync(stream);

        if (image.Size != desiredSize)
        {
            var width = (desiredSize.Width & 1) == 0 ? desiredSize.Width : desiredSize.Width - 1;
            var height = (desiredSize.Height & 1) == 0 ? desiredSize.Height : desiredSize.Height - 1;

            image.Mutate(x => x.Resize(width, height));
        };

        await image.SaveAsync(imageSaveInfo.SavedFileNameWithExtension);
    }

    private async Task InsertFrame(UploadContext context, FfmpegContext ffmpegContext)
    {
        await TransferImageIntoVideo(context.ImageSaveInfo.SavedFileNameWithExtension, ffmpegContext.TempImageVideoFilePath);
        await CreateInputTextFile(context.VideoSaveInfo.SavedFileNameWithExtension, context.ImageSaveInfo.SavedFileNameWithExtension, ffmpegContext.InputFilePath);
        await ConcatStream(ffmpegContext);
    }

    private async Task TransferImageIntoVideo(string imageFullPath, string outputFullPath)
    {
        var argument = $"-loop 1 -i {imageFullPath} -c:v libx264 -t 0.17 -pix_fmt yuv420p {outputFullPath}";
        var process = CreateFfmpegProcess(argument);
        process.Start();
        await process.WaitForExitAsync();
    }

    private async Task CreateInputTextFile(string videoFileFullPath, string imageVideoFileFullPath, string inputFilePath)
    {
        var content = $"file '{videoFileFullPath}'\nfile '{imageVideoFileFullPath}'";

        await System.IO.File.WriteAllTextAsync(inputFilePath, content, Encoding.UTF8);
    }

    private async Task ConcatStream(FfmpegContext ffmpegContext)
    {
        var argument = $"-f concat -i {ffmpegContext.InputFilePath} -c copy -movflags +faststart {ffmpegContext.OutputFilePath}";
        _logger.LogInformation(argument);
        var process = CreateFfmpegProcess(argument);
        process.Start();
        await process.WaitForExitAsync();
    }

    private async Task SaveFormFile(IFormFile file, FileSaveInfo fileSaveInfo)
    {
        using var fileStream = new FileStream(fileSaveInfo.SavedFileNameWithExtension, FileMode.Create);
        await file.CopyToAsync(fileStream);
    }

    private Process CreateFfmpegProcess(string arguments)
    {
        var process = new Process();

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.FileName = "ffmpeg";
        process.StartInfo.Arguments = arguments;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;

        return process;
    }

    private string GetCurrentTimeStamp()
    {
        string unixTimestamp = Convert.ToString((int)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
        return unixTimestamp;
    }

    public record UploadContext
    {
        public FileSaveInfo ImageSaveInfo { set; get; }
        public FileSaveInfo VideoSaveInfo { set; get; }
        public FfmpegContext IntermediateFile { set; get; }
        public string CurrentTimestamp { set; get; }

        public UploadContext(string tempPath,
            string timeStamp,
            string videoFileNameWithExtension,
            string imageFileNameWithExtension)
        {

            VideoSaveInfo = FilePathHelper.CreateUploadedMediaMetadata(tempPath, videoFileNameWithExtension, timeStamp);
            ImageSaveInfo = FilePathHelper.CreateUploadedMediaMetadata(tempPath, imageFileNameWithExtension, timeStamp);

            CurrentTimestamp = timeStamp;
        }
    }

    public record FileSaveInfo
    {
        public string FileNameWithoutExtension { set; get; }
        public string FileNameWithExtension { set; get; }
        public string SavedFileNameWithoutExtension { set; get; }
        public string SavedFileNameWithExtension { set; get; }
        public string FileExtension { set; get; }
        // public Size Size { set; get; }
    }

    public class FfmpegContext
    {
        public string TempImageVideoFilePath { set; get; }
        public string InputFilePath { set; get; }
        public string OutputFilePath { set; get; }
        public string CreateInputFileContent(string videoFilePath)
        {
            return $"file '{videoFilePath}\nfile '{TempImageVideoFilePath}'";
        }

        // public string Create

        public FfmpegContext(string tempPath,
         string outputPath,
         UploadContext context,
         string timeStamp)
        {
            InputFilePath = FilePathHelper.GetPathToSave(tempPath, FilePathHelper.ConstructFileNameToSave("files_container", timeStamp));
            TempImageVideoFilePath = FilePathHelper.GetPathToSave(tempPath, $"{FilePathHelper.ConstructFileNameToSave(context.ImageSaveInfo.FileNameWithoutExtension, timeStamp)}.mp4");
            OutputFilePath = FilePathHelper.GetPathToSave(outputPath, $"{FilePathHelper.ConstructFileNameToSave("output", timeStamp)}.mp4");
        }
    }

    public static class FilePathHelper
    {

        public static FileSaveInfo CreateUploadedMediaMetadata(string savePath, string filenameWithExtension, string timeStamp)
        {
            return new FileSaveInfo()
            {
                FileNameWithExtension = filenameWithExtension,
                FileNameWithoutExtension = Path.GetFileNameWithoutExtension(filenameWithExtension),
                FileExtension = Path.GetExtension(filenameWithExtension),
                SavedFileNameWithExtension = Path.Combine(savePath, ConstructFileNameToSave(filenameWithExtension, timeStamp))
            };
        }

        public static string GetPathToSave(string basePath, string fileName)
        {
            return Path.Combine(basePath, fileName);
        }

        public static string ConstructFileNameToSave(string fileName, string timeStamp)
        {
            return $"{timeStamp}_{fileName}";
        }
    }
}