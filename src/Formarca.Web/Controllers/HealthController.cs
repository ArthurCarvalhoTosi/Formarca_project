using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Formarca.Web.Controllers;

[ApiController]
[Route("api")]
public sealed class HealthController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "Healthy", utc = DateTime.UtcNow });

    [AllowAnonymous]
    [HttpGet("version")]
    public IActionResult Version()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        return Ok(new { name = "Formarca", version });
    }
}
