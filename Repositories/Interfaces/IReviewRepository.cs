using BusinessObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TMPMS.Repositories.Interfaces
{
    public interface IReviewRepository
    {
        Task<List<Review>> GetByMedicineIdAsync(int medicineId);
        Task<bool> HasPurchasedAsync(int userId, int medicineId);
        Task<bool> HasReviewedAsync(int userId, int medicineId);
        Task<Review> CreateAsync(Review review);
        Task<string?> GetUserDisplayNameAsync(int userId);
        Task<string?> GetUserNameAsync(int userId);
    }
}
