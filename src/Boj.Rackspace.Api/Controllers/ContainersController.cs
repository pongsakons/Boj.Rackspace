using Microsoft.AspNetCore.Mvc;

namespace Boj.Rackspace.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContainersController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new[] { "Container1", "Container2", "Container3" });
        }
    }
}
