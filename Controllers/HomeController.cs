using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using tikthumb.Models;

namespace tikthumb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly string _filePath;
    private readonly string _outputPath;
    private readonly string _currentTimeStamp;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _filePath = configuration["SaveLocation"];
        _outputPath = _filePath + "/output";
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

        var videoFile = await SaveFormFile(files["videoFile"]);

        var thumbnailFile = await SaveFormFile(files["thumbnailFile"]);

        await InsertFrame(videoFile, thumbnailFile);
        //return stream
        return View();
    }

    private async Task InsertFrame(FileSavedInfo videoFile, FileSavedInfo imageFile)
    {
        await TransferImageIntoVideo(imageFile);
        //create txt file that contains file list
        // await CreateInputTextFile(videoFile, imageFile);
        // //concat stream
        // await ConcatStream(videoFile, imageFile, MakeFileInfoPath(_outputPath, _currentTimeStamp));
    }

    private async Task TransferImageIntoVideo(FileSavedInfo fileInfo)
    {
        var argument = $"-loop 1 -i {fileInfo.FullSavedPath} -c:v libx264 -t 0.17 -pix_fmt yuv420p {fileInfo.SavedFileNameWithoutExtension}.mp4";
        var process = CreateFfmpegProcess(argument);
        process.Start();
        await process.WaitForExitAsync();
    }

    private async Task CreateInputTextFile(FileSavedInfo videoFile, FileSavedInfo imageFile)
    {
        var outputPath = MakeFileInfoPath(_outputPath, _currentTimeStamp);
        var content = $"file '{videoFile.FullSavedPath}'\nfile '{_filePath}/{imageFile.SavedFileNameWithoutExtension}.mp4'";

        await System.IO.File.WriteAllTextAsync(outputPath, content, Encoding.UTF8);
    }

    private async Task ConcatStream(FileSavedInfo videoFile, FileSavedInfo imageFile, string textFile)
    {
        var argument = $"-f concat -i {textFile} -c copy -movflags +faststart {_outputPath}/{videoFile.SavedFileNameWithoutExtension}.mp4";
        _logger.LogInformation(argument);
        var process = CreateFfmpegProcess(argument);
        process.Start();
        await process.WaitForExitAsync();
    }

    private async Task<FileSavedInfo> SaveFormFile(IFormFile file)
    {
        var (savePath, newFileName) = GetFilePathToSave(_filePath, file.FileName, _currentTimeStamp);
        using var videoFileStream = new FileStream(savePath, FileMode.Create);
        await file.CopyToAsync(videoFileStream);

        return new FileSavedInfo
        {
            FileExtension = Path.GetExtension(file.FileName),
            SavedFileNameWithoutExtension = newFileName,
            FullSavedPath = savePath
        };
    }

    private (string FullPath, string FileName) GetFilePathToSave(string basePath, string fileNameWithExtension, string timeStamp)
    {
        var fileExtension = Path.GetExtension(fileNameWithExtension);
        var fileName = Path.GetFileNameWithoutExtension(fileNameWithExtension);
        if (fileName.Length > 100)
        {
            fileName = fileName.Substring(0, 100);
        }

        return ($"{basePath}/{timeStamp}_{fileName}{fileExtension}", $"{timeStamp}_{fileName}");
    }

    private Process CreateFfmpegProcess(string arguments)
    {
        var process = new Process();

        process.StartInfo.UseShellExecute = true;
        process.StartInfo.FileName = "ffmpeg";
        process.StartInfo.Arguments = arguments;
        process.StartInfo.CreateNoWindow = true;

        return process;
    }

    private string GetCurrentTimeStamp()
    {
        string unixTimestamp = Convert.ToString((int)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
        return unixTimestamp;
    }

    private string MakeFileInfoPath(string outputPath, string timeStamp)
    {
        return $"{outputPath}/fileinfo_{timeStamp}.txt";
    }

    public class FileSavedInfo
    {
        public string OriginalFileNameWithoutExtension { set; get; }
        public string SavedFileNameWithoutExtension { set; get; }
        public string FileExtension { set; get; }
        public string FullSavedPath { set; get; }
    }

    public class VideoInfo
    {
        public StreamInfo StreamInfo { set; get; }
    }

    public class StreamInfo
    {
        public int Width { set; get; }
        public int Height { set; get; }
    }
}
