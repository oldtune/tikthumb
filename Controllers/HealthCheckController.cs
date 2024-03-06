using Microsoft.AspNetCore.Mvc;

namespace tikthumb.Controllers;

[ApiController]
[Route("healthcheck")]
public class HealthCheckController : ControllerBase
{
    public HealthCheckController(ILogger<HealthCheckController> logger)
    {
    }

    [HttpGet("")]
    public async Task<IActionResult> HealthCheck()
    {
        //return Ok to bypass AWS health check for now
        return Ok();
    }
}