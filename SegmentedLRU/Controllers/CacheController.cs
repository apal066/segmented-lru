using Microsoft.AspNetCore.Mvc;
using SegmentedLRU.Models;
using SegmentedLRU.Services;

namespace SegmentedLRU.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CacheController : Controller
    {
        private readonly ICachingService<User> _cachingService;

        public CacheController(ICachingService<User> cachingService)
        {
            _cachingService = cachingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetStatus()
        {
            return Ok(await _cachingService.GetCurrentCacheKeys());
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCache()
        {
            await _cachingService.ClearCache();
            return Ok();
        }
    }
}
