using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Boj.Rackspace.Infrastructure.Examples.TokenRefreshSystemExample;

namespace Boj.Rackspace.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContainersController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            CacheReadExample cacheReadExample = new CacheReadExample();
            cacheReadExample.DemonstrateCacheRead();
            return Ok(new[] { "Container1", "Container2", "Container3" });
        }
    }
}
