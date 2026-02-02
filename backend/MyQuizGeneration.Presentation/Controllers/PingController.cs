using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MyQuizGeneration.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Ping()
        {
            return Ok("Pong");
        }
    }
}
