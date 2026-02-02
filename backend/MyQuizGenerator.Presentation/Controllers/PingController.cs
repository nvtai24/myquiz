using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Mvc;

namespace MyQuizGenerator.Presentation.Controllers;

[Route("api/[controller]")]
public class PingController : BaseApiController
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult Ping()
    {
        return ApiOk(new { Reply = "Pong", ServerTime = DateTime.UtcNow }, "Server is running");
    }
}
