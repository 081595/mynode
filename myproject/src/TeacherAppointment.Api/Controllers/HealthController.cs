using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace TeacherAppointment.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "ok" });
    }

    [Authorize]
    [HttpGet("secure")]
    public IActionResult GetSecure()
    {
        return Ok(new { status = "authorized" });
    }
}
