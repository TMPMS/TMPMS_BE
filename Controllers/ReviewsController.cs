using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using TMPMS.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly TMPMSDbContext _context;

        public ReviewsController(TMPMSDbContext context)
        {
            _context = context;
        }

        [HttpGet("medicine/{medicineId}")]
        public async Task<IActionResult> GetReviews(int medicineId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.MedicineId == medicineId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    r.Id,
                    r.UserId,
                    Username = _context.Users.Where(u => u.Id == r.UserId).Select(u => u.UserName).FirstOrDefault() ?? "Người dùng",
                    r.Rating,
                    r.Comment,
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpGet("check-eligibility")]
        [Authorize]
        public async Task<IActionResult> CheckEligibility([FromQuery] int medicineId, [FromQuery] int userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();
            if (!CanProxy() && userId != currentUserId.Value) return Forbid();

            var hasPurchased = await _context.Orders
                .Where(o => o.UserId == userId)
                .AnyAsync(o => o.OrderItems.Any(oi => oi.MedicineId == medicineId));

            return Ok(new { eligible = hasPurchased });
        }

        public class CreateReviewInput
        {
            public int UserId { get; set; }
            public int MedicineId { get; set; }
            public int Rating { get; set; }
            public string Comment { get; set; } = "";
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewInput input)
        {
            if (input.Rating < 1 || input.Rating > 5)
            {
                return BadRequest("Đánh giá phải từ 1 đến 5 sao.");
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();
            if (!CanProxy() && input.UserId != currentUserId.Value) return Forbid();
            var userId = CanProxy() && input.UserId != 0 ? input.UserId : currentUserId.Value;

            var hasPurchased = await _context.Orders
                .Where(o => o.UserId == userId)
                .AnyAsync(o => o.OrderItems.Any(oi => oi.MedicineId == input.MedicineId));

            if (!hasPurchased)
            {
                return BadRequest("Bạn chỉ có thể đánh giá sản phẩm sau khi đã mua hàng thành công.");
            }

            var review = new Review
            {
                UserId = userId,
                MedicineId = input.MedicineId,
                Rating = input.Rating,
                Comment = input.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return StatusCode(201, new {
                review.Id,
                review.UserId,
                Username = _context.Users.Where(u => u.Id == review.UserId).Select(u => u.UserName).FirstOrDefault() ?? "Người dùng",
                review.Rating,
                review.Comment,
                review.CreatedAt
            });
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
