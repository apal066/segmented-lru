using Microsoft.AspNetCore.Mvc;
using SegmentedLRU.Models;
using SegmentedLRU.Services;

namespace SegmentedLRU.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly ICachingService<User> _cachingService;

        public UserController(ILogger<UserController> logger,
            ICachingService<User> cachingService)
        {
            _logger = logger;
            _cachingService = cachingService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string key)
        {
            var user = await _cachingService.GetAsync(key);
            if (user == null)
                return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> Set(User user)
        {
            await _cachingService.SetAsync(user.Id.ToString(), user);
            return Ok();
        }
    }
}