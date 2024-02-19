using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace tikthumb.Controllers;

public class HomeController : Controller
{
    readonly static string[] AllowedVideoFileType = [".mov", ".avi", ".wmv", ".avchd", ".webm", ".flv", ".mkv", ".amv", ".mp4"];
    readonly static string[] AllowedImageFileType = [".png", ".tiff", ".raw", ".jpg", ".webp", ".bmp", ".jpeg"];
    private readonly ILogger<HomeController> _logger;
    private readonly string _tempPath;
    private readonly string _currentTimeStamp;
    private readonly UploadContext _uploadContext;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _tempPath = configuration["SaveLocation"];
        _currentTimeStamp = GetCurrentTimeStamp();
    }

    [AcceptVerbs("GET")]
    [ActionName("Index")]
    public IActionResult Index()
    {
        return View();
    }

    [ValidateAntiForgeryToken]
    [AcceptVerbs("POST")]
    [ActionName("Index")]
    [AllowAnonymous]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> Post()
    {
        var files = Request.Form.Files;

        var uploadedVideoFile = files["videoFile"];
        var uploadedThumbnailFile = files["thumbnailFile"];

        var validateResult = ValidateFiles(uploadedVideoFile, uploadedThumbnailFile);

        if (!validateResult)
        {
            _logger.LogError("Validation failed!");
            return View();
        }

        var uploadContext = new UploadContext(_tempPath,
            _currentTimeStamp,
            FilePathHelper.NormalizeFileName(uploadedVideoFile!.FileName),
            FilePathHelper.NormalizeFileName(uploadedThumbnailFile!.FileName));

        await SaveFormFile(files["videoFile"]!, uploadContext.VideoSaveInfo);

        var videoSize = await GetVideoSize(uploadContext.VideoSaveInfo.SavedFileNameWithExtension);

        await ResizeAndSaveThumbnail(uploadContext.ImageSaveInfo, uploadedThumbnailFile, videoSize);

        var tempDataContext = new FfmpegContext(_tempPath, uploadContext, _currentTimeStamp);

        await InsertFrame(uploadContext, tempDataContext);

        var stream = new FileStream(tempDataContext.FinalOutputFilePath, FileMode.Open);
        return File(stream, "application/octet", tempDataContext.FinalOutputFileName);
    }

    private bool ValidateFiles(IFormFile videoFile, IFormFile thumbnailFile)
    {
        return (AllowedVideoFileType.Contains(Path.GetExtension(videoFile.FileName))
        && AllowedImageFileType.Contains(Path.GetExtension(thumbnailFile.FileName)));
    }

    private async Task<string> GetVideoLength(string fullVideoName)
    {
        var process = CreateFfmpegProcess($"-i {fullVideoName} -show_entries format=duration -v quiet -of csv=\"p=0\" -sexagesimal", ffprobe: true);
        process.Start();
        var stdOutput = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        return stdOutput;
    }

    private async Task<Size> GetVideoSize(string videoFileName)
    {
        var argument = $"-v error -select_streams v:0 -show_entries stream=width,height -of csv=p=0 {videoFileName}";
        var process = CreateFfmpegProcess(argument, ffprobe: true);
        process.Start();

        var stdOutput = await process.StandardOutput.ReadToEndAsync();

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
        await TransferImageIntoVideo(context.VideoSaveInfo.SavedFileNameWithExtension, context.ImageSaveInfo.SavedFileNameWithExtension, ffmpegContext.TempImageVideoFilePath);

        await CreateInputTextFile(context.VideoSaveInfo.SavedFileName, ffmpegContext.TempImageVideoFileName, ffmpegContext.InputFilePath);

        await ConcatStream(ffmpegContext, (context) => context.OutputFilePath);

        await CutEnd(ffmpegContext, context);

        await CreateInputTextFile(ffmpegContext.CuttedEndPartFileName, context.VideoSaveInfo.SavedFileName, ffmpegContext.InputFilePath);

        await ConcatStream(ffmpegContext, (context) => context.FinalOutputFilePath);
    }

    private async Task TransferImageIntoVideo(string videoFullPath, string imageFullPath, string outputFullPath)
    {
        var preset = await GetVideoPreset(videoFullPath);
        _logger.LogInformation(preset);
        // var argument = $"-loop 1 -i {imageFullPath} -f lavfi -i anullsrc=channel_layout=stereo:sample_rate=44100 -c:v libx264 -t 3 -r 60 -shortest -preset slow {outputFullPath}";
        var argument = $"-loop 1 -i {imageFullPath} -t 3 -c:v copy -preset {preset} {outputFullPath}";
        var process = CreateFfmpegProcess(argument);
        process.Start();
        await process.WaitForExitAsync();
    }

    private async Task<string> GetVideoPreset(string videoPath)
    {
        var argument = $"-select_streams v:0 -show_entries stream=codec_name,codec_long_name -v quiet ${videoPath}";
        var process = CreateFfmpegProcess(argument, ffprobe: true);
        process.Start();

        var stdOutput = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        return stdOutput;
    }

    private async Task CreateInputTextFile(string videoFileName, string imageVideoName, string inputFilePath)
    {
        // var content = $"file '{imageVideoName}'\nfile '{videoFileName}'";
        var content = $"file '{videoFileName} \nfile '{imageVideoName}'";

        Encoding utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        await System.IO.File.WriteAllTextAsync(inputFilePath, content, utf8WithoutBom);
    }

    private async Task ConcatStream(FfmpegContext ffmpegContext, Func<FfmpegContext, string> outFilePath)
    {
        var argument = $"-f concat -safe 0 -i {ffmpegContext.InputFilePath} -c copy {outFilePath(ffmpegContext)}";
        var process = CreateFfmpegProcess(argument);
        process.Start();
        await process.WaitForExitAsync();
    }

    private async Task SaveFormFile(IFormFile file, FileSaveInfo fileSaveInfo)
    {
        using var fileStream = new FileStream(fileSaveInfo.SavedFileNameWithExtension, FileMode.Create);
        await file.CopyToAsync(fileStream);
    }

    private async Task SplitVideoStream(UploadContext context, FfmpegContext ffmpegContext)
    {

    }

    private async Task CutEnd(FfmpegContext ffmpegContext, UploadContext uploadContext)
    {
        var videoLength = await GetVideoLength(uploadContext.VideoSaveInfo.SavedFileNameWithExtension);
        var formattedVideoLength = FormatVideoDuration(videoLength);
        _logger.LogInformation($"Video length is {formattedVideoLength}");

        var outputVideoLength = await GetVideoLength(ffmpegContext.OutputFilePath);
        var formattedOutputVideoLength = FormatVideoDuration(outputVideoLength);
        _logger.LogInformation($"New video length is {formattedOutputVideoLength}");

        var argument = $"-i {ffmpegContext.OutputFilePath} -ss {formattedVideoLength} -to {formattedOutputVideoLength} -acodec copy -vcodec libx264 {ffmpegContext.CuttedEndPartFilePath}";

        var process = CreateFfmpegProcess(argument);

        process.Start();

        await process.WaitForExitAsync();
    }

    private Process CreateFfmpegProcess(string arguments, bool ffprobe = false)
    {
        var process = new Process();

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.FileName = ffprobe ? "ffprobe" : "ffmpeg";
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

    private string FormatVideoDuration(string duration)
    {
        var parts = duration.Split(':');
        var seconds = parts[2].Split('.');
        return $"{int.Parse(parts[0]).ToString("00")}:{int.Parse(parts[1]).ToString("00")}:{int.Parse(seconds[0]).ToString("00")}.{seconds[1].Substring(0, 3)}";
    }

    public record UploadContext
    {
        public FileSaveInfo ImageSaveInfo { set; get; }
        public FileSaveInfo VideoSaveInfo { set; get; }
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
        public string SavedFileNameWithExtension { set; get; }
        public string SavedFileName { set; get; }
        public string FileExtension { set; get; }
    }

    public class FfmpegContext
    {
        public string CuttedEndPartFileName { set; get; }
        public string CuttedEndPartFilePath { set; get; }
        public string FinalOutputFileName { set; get; }
        public string FinalOutputFilePath { set; get; }
        public string TempImageVideoFilePath { set; get; }
        public string TempImageVideoFileName { set; get; }
        public string InputFilePath { set; get; }
        public string OutputFilePath { set; get; }
        public string OutputFileName { set; get; }

        public FfmpegContext(string tempPath,
         UploadContext context,
         string timeStamp)
        {
            TempImageVideoFileName = $"{FilePathHelper.ConstructFileNameToSave(context.ImageSaveInfo.FileNameWithoutExtension, timeStamp)}{context.VideoSaveInfo.FileExtension}";

            InputFilePath = FilePathHelper.GetPathToSave(tempPath, FilePathHelper.ConstructFileNameToSave("files_container", timeStamp));
            TempImageVideoFilePath = FilePathHelper.GetPathToSave(tempPath, TempImageVideoFileName);

            OutputFileName = $"{FilePathHelper.ConstructFileNameToSave("output", timeStamp)}{context.VideoSaveInfo.FileExtension}";
            OutputFilePath = FilePathHelper.GetPathToSave(tempPath, OutputFileName);

            CuttedEndPartFileName = $"{FilePathHelper.ConstructFileNameToSave("end", timeStamp)}{context.VideoSaveInfo.FileExtension}";
            CuttedEndPartFilePath = FilePathHelper.GetPathToSave(tempPath, CuttedEndPartFileName);

            FinalOutputFileName = $"{FilePathHelper.ConstructFileNameToSave("final", timeStamp)}{context.VideoSaveInfo.FileExtension}";
            FinalOutputFilePath = FilePathHelper.GetPathToSave(tempPath, $"{FinalOutputFileName}");
        }
    }

    public static class FilePathHelper
    {

        public static FileSaveInfo CreateUploadedMediaMetadata(string savePath, string filenameWithExtension, string timeStamp)
        {
            var newFileName = ConstructFileNameToSave(filenameWithExtension, timeStamp);
            return new FileSaveInfo()
            {
                FileNameWithExtension = filenameWithExtension,
                FileNameWithoutExtension = Path.GetFileNameWithoutExtension(filenameWithExtension),
                FileExtension = Path.GetExtension(filenameWithExtension),
                SavedFileNameWithExtension = Path.Combine(savePath, newFileName),
                SavedFileName = newFileName
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

        public static string NormalizeFileName(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in fileName)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}