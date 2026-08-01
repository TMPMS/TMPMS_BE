using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Threading.Tasks;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class HealthReelsController : ControllerBase
    {
        private readonly IHealthReelsService _healthReelsService;

        public HealthReelsController(IHealthReelsService healthReelsService)
        {
            _healthReelsService = healthReelsService;
        }

        /// <summary>
        /// GET /api/HealthReels/videos
        /// Lấy danh sách video sức khỏe YouTube được cache an toàn qua Backend Proxy
        /// </summary>
        [HttpGet("videos")]
        public async Task<IActionResult> GetVideos()
        {
            var result = await _healthReelsService.GetHealthReelsAsync();
            return Ok(result);
        }
    }
}
