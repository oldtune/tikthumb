using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using tikthumb.Ffmpeg;

namespace tikthumb.Controllers;

public class TestController : Controller
{
    readonly ILogger<TestController> _logger;
    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [ActionName("Index")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Index")]
    public IActionResult TestPost()
    {
        _logger.LogError("Yep");
        return View();
    }

    [HttpPost]
    [Route("test-json")]
    public IActionResult TestPostJson([FromBody] string json)
    {
        var result = JsonConvert.DeserializeObject<FfprobeVideoInfo>(json);
        return Ok(result);
    }
}