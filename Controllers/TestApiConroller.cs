using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using tikthumb.Ffmpeg;

namespace tikthumb.Controllers;

[ApiController]
[Route("test-api")]
public class TestApiController : ControllerBase
{
    public TestApiController()
    {
    }

    [HttpPost("json")]
    public IActionResult TestPostJson([FromBody] string json)
    {
        var result = JsonConvert.DeserializeObject<FfprobeVideoInfo>(json);
        return Ok(result);
    }
}