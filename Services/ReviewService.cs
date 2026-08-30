using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects;
using TMPMS.DTOs;
using TMPMS.Repositories.Interfaces;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _repo;
        public ReviewService(IReviewRepository repo) => _repo = repo;

        public async Task<List<ReviewResponseDto>> GetByMedicineIdAsync(int medicineId)
        {
            var reviews = await _repo.GetByMedicineIdAsync(medicineId);
            var result = new List<ReviewResponseDto>();
            foreach (var r in reviews)
            {
                result.Add(new ReviewResponseDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    Username = await _repo.GetUserDisplayNameAsync(r.UserId) ?? "Khách hàng",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                });
            }
            return result;
        }

        public async Task<bool> CheckEligibilityAsync(int userId, int medicineId)
        {
            // Đã mua VÀ chưa từng review sản phẩm này — trước đây chỉ check đã mua, cho phép 1 user
            // review vô hạn lần trên cùng 1 sản phẩm để thao túng rating trung bình.
            var hasPurchased = await _repo.HasPurchasedAsync(userId, medicineId);
            if (!hasPurchased) return false;
            return !await _repo.HasReviewedAsync(userId, medicineId);
        }

        public async Task<(ReviewResponseDto? Review, string? Error)> CreateAsync(ReviewCreateDto dto)
        {
            if (dto.Rating < 1 || dto.Rating > 5)
            {
                return (null, "Đánh giá phải từ 1 đến 5 sao.");
            }

            var hasPurchased = await _repo.HasPurchasedAsync(dto.UserId, dto.MedicineId);
            if (!hasPurchased)
            {
                return (null, "Bạn chỉ có thể đánh giá sản phẩm sau khi đã mua hàng thành công.");
            }

            if (await _repo.HasReviewedAsync(dto.UserId, dto.MedicineId))
            {
                return (null, "Bạn đã đánh giá sản phẩm này rồi.");
            }

            var review = new Review
            {
                UserId = dto.UserId,
                MedicineId = dto.MedicineId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repo.CreateAsync(review);

            return (new ReviewResponseDto
            {
                Id = created.Id,
                UserId = created.UserId,
                Username = await _repo.GetUserNameAsync(created.UserId) ?? "Người dùng",
                Rating = created.Rating,
                Comment = created.Comment,
                CreatedAt = created.CreatedAt
            }, null);
        }
    }
}
