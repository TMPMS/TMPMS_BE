using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Security.Claims;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _service;

        public ReviewsController(IReviewService service)
        {
            _service = service;
        }

        [HttpGet("medicine/{medicineId}")]
        public async Task<IActionResult> GetReviews(int medicineId)
        {
            var reviews = await _service.GetByMedicineIdAsync(medicineId);
            return Ok(reviews);
        }

        [HttpGet("check-eligibility")]
        [Authorize]
        public async Task<IActionResult> CheckEligibility([FromQuery] int medicineId, [FromQuery] int userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();
            if (!CanProxy() && userId != currentUserId.Value) return Forbid();

            var eligible = await _service.CheckEligibilityAsync(userId, medicineId);
            return Ok(new { eligible });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReview([FromBody] ReviewCreateDto input)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();
            if (!CanProxy() && input.UserId != currentUserId.Value) return Forbid();
            input.UserId = CanProxy() && input.UserId != 0 ? input.UserId : currentUserId.Value;

            var (review, error) = await _service.CreateAsync(input);
            if (error != null) return BadRequest(error);

            return StatusCode(201, review);
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : (int?)null;
        }

        private bool CanProxy()
        {
            return User.IsInRole("Admin") || User.IsInRole("Staff") || User.IsInRole("Pharmacy");
        }
    }
}
