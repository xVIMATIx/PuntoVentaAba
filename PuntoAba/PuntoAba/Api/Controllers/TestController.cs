using Microsoft.AspNetCore.Mvc;

namespace PuntoAba.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "PONG", timestamp = DateTime.UtcNow });
        }
    }
}
