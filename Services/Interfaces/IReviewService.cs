using System.Collections.Generic;
using System.Threading.Tasks;
using TMPMS.DTOs;

namespace TMPMS.Services.Interfaces
{
    public interface IReviewService
    {
        Task<List<ReviewResponseDto>> GetByMedicineIdAsync(int medicineId);
        Task<bool> CheckEligibilityAsync(int userId, int medicineId);
        Task<(ReviewResponseDto? Review, string? Error)> CreateAsync(ReviewCreateDto dto);
    }
}
