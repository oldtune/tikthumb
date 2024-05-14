using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using tikthumb.Ffmpeg;

namespace tikthumb.Controllers;

public class HomeController : Controller
{
    readonly static string[] AllowedVideoFileType = [".mov", ".avi", ".wmv", ".webm", ".flv", ".mkv", ".amv", ".mp4"];
    readonly static string[] AllowedImageFileType = [".png", ".tiff", ".raw", ".jpg", ".webp", ".bmp", ".jpeg"];
    private readonly ILogger<HomeController> _logger;
    private readonly string _tempPath;
    private readonly string _currentTimeStamp;

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
        _logger.LogInformation("Someone view our page");
        return View();
    }

    [ValidateAntiForgeryToken]
    [AcceptVerbs("POST")]
    [ActionName("Index")]
    [AllowAnonymous]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> Post()
    {
        _logger.LogInformation("Someone actually tries our service");
        var files = Request.Form.Files;

        var uploadedVideoFile = files["videoFile"];
        var uploadedThumbnailFile = files["thumbnailFile"];

        var validateResult = ValidateFiles(uploadedVideoFile, uploadedThumbnailFile);

        if (!validateResult)
        {
            _logger.LogError("Validation failed!");
            return BadRequest("Bad file extensions");
        }

        var uploadContext = new UploadContext(_tempPath,
            _currentTimeStamp,
            FilePathHelper.NormalizeFileName(uploadedVideoFile!.FileName),
            FilePathHelper.NormalizeFileName(uploadedThumbnailFile!.FileName));

        await SaveFormFile(files["videoFile"]!, uploadContext.VideoSaveInfo);

        var videoSize = await GetVideoResolution(uploadContext.VideoSaveInfo.SavedFileNameWithExtension);

        await ResizeAndSaveThumbnail(uploadContext.ImageSaveInfo, uploadedThumbnailFile, videoSize);

        var ffmpegContext = new FfmpegContext(_tempPath, uploadContext, _currentTimeStamp);

        await InsertFrame(uploadContext, ffmpegContext);

        CleanUp(uploadContext, ffmpegContext);

        var stream = new FileStream(ffmpegContext.OutputFilePath, FileMode.Open);
        _logger.LogInformation("Success trying our service! :happy:");
        return new CustomFileStreamResult(stream, ffmpegContext.OutputFilePath, ffmpegContext.OutputFileName);
    }

    public void CleanUp(UploadContext uploadContext, FfmpegContext tempContext)
    {
        System.IO.File.Delete(uploadContext.VideoSaveInfo.SavedFileNameWithExtension);
        System.IO.File.Delete(uploadContext.ImageSaveInfo.SavedFileNameWithExtension);
        System.IO.File.Delete(tempContext.TempImageVideoFilePath);
        System.IO.File.Delete(tempContext.InputFilePath);
    }

    private bool ValidateFiles(IFormFile videoFile, IFormFile thumbnailFile)
    {
        return (AllowedVideoFileType.Contains(Path.GetExtension(videoFile.FileName))
        && AllowedImageFileType.Contains(Path.GetExtension(thumbnailFile.FileName)));
    }

    private async Task<Size> GetVideoResolution(string videoFileName)
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
    }

    private async Task TransferImageIntoVideo(string videoFullPath, string imageFullPath, string outputFullPath)
    {
        var videoMetadata = await GetVideoMetadata(videoFullPath);

        var arguments = $"-loop 1 -i {imageFullPath} -f lavfi -i anullsrc=channel_layout=stereo:sample_rate={videoMetadata.AudioStreamInfo?.SampleRate ?? "44100"} -ac {videoMetadata.AudioStreamInfo?.Channels ?? "2"} -c:a {videoMetadata.AudioStreamInfo?.CodecName ?? "aac"} -b:a {videoMetadata.AudioStreamInfo?.BitRate ?? "128011"} -c:v {videoMetadata.VideoStreamInfo.CodecName} -level:v {videoMetadata.VideoStreamInfo.Level} -b:v {videoMetadata.VideoStreamInfo.BitRate} -pix_fmt {videoMetadata.VideoStreamInfo.PixelFormat} -t 0.01 -r {videoMetadata.VideoStreamInfo.AverageFrameRate} -profile:v {videoMetadata.VideoStreamInfo.Profile.ToLower()} {outputFullPath}";

        _logger.LogInformation(arguments);

        var process = CreateFfmpegProcess(arguments);
        process.Start();
        await process.WaitForExitAsync();
    }

    private async Task<FfprobeVideoInfo> GetVideoMetadata(string videoPath)
    {
        var argument = $"-v quiet -print_format json -show_streams {videoPath}";
        var process = CreateFfmpegProcess(argument, ffprobe: true);
        process.Start();

        var stdOutput = await process.StandardOutput.ReadToEndAsync();
        _logger.LogInformation(stdOutput);

        var videoInfo = JsonConvert.DeserializeObject<FfprobeVideoInfo>(stdOutput);
        return videoInfo;
    }

    private async Task CreateInputTextFile(string videoFileName, string imageVideoName, string inputFilePath)
    {
        var content = $"file '{imageVideoName}'\nfile '{videoFileName}'";
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

    /* [HttpGet] */
    /* [ActionName("contact")] */
    /* public IActionResult Contact() */
    /* { */
    /*     return View(); */
    /* } */

    /* [HttpGet] */
    /* [ActionName("about")] */
    /* public IActionResult About() */
    /* { */
    /*     return View(); */
    /* } */

    /* [HttpGet] */
    /* [ActionName("BugReport")] */
    /* public IActionResult BugReport() */
    /* { */
    /*     return View(); */
    /* } */

    /* [HttpPost] */
    /* [ActionName("BugReport")] */
    /* public IActionResult PostBugReport() */
    /* { */
    /*     return View(); */
    /* } */

    /* [HttpGet] */
    /* [ActionName("FeatureRequest")] */
    /* public IActionResult GetFeatureRequestPage() */
    /* { */
    /*     return View(); */
    /* } */

    /* [HttpPost] */
    /* [ActionName("FeatureRequest")] */
    /* public IActionResult PostFeatureRequest() */
    /* { */
    /*     return View(); */
    /* } */

    /* [HttpGet] */
    /* [ActionName("Pricing")] */
    /* public IActionResult GetPricing() */
    /* { */
    /*     return View(); */
    /* } */

    /* [HttpPost] */
    /* [ActionName("Pricing")] */
    /* public IActionResult PostPricing() */
    /* { */
    /*     return View(); */
    /* } */
}
