using System.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using tikthumb.Models;

namespace tikthumb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly string _filePath;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _filePath = configuration["SaveLocation"];
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
        //process the video
        var videoFile = files["videoFile"];
        var videoSavePath = GetFilePathToSave(_filePath, videoFile.FileName, GetCurrentTimeStamp());
        using var videoFileStream = new FileStream(videoSavePath, FileMode.Create);
        await videoFile.CopyToAsync(videoFileStream);

        //process the image
        var thumbnailFile = files["thumbnailFile"];


        return View();
    }

    private string GetFilePathToSave(string basePath, string fileNameWithExtension, string timeStamp)
    {
        var fileExtension = Path.GetExtension(fileNameWithExtension);
        var fileName = Path.GetFileNameWithoutExtension(fileNameWithExtension);
        if (fileName.Length > 100)
        {
            fileName = fileName.Substring(0, 100);
        }
        return $"{basePath}/{timeStamp}/{fileName}/{fileExtension}";
    }

    private string GetCurrentTimeStamp()
    {
        string unixTimestamp = Convert.ToString((int)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
        return unixTimestamp;
    }
}
