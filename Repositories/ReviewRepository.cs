using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPMS.Data;
using TMPMS.Repositories.Interfaces;

namespace TMPMS.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly TMPMSDbContext _context;
        public ReviewRepository(TMPMSDbContext context) => _context = context;

        public async Task<List<Review>> GetByMedicineIdAsync(int medicineId)
        {
            return await _context.Reviews
                .Where(r => r.MedicineId == medicineId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> HasPurchasedAsync(int userId, int medicineId)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId)
                .AnyAsync(o => o.OrderItems.Any(oi => oi.MedicineId == medicineId));
        }

        public async Task<Review> CreateAsync(Review review)
        {
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task<string?> GetUserDisplayNameAsync(int userId)
        {
            return await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.UserName)
                .FirstOrDefaultAsync();
        }

        public async Task<string?> GetUserNameAsync(int userId)
        {
            return await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync();
        }
    }
}
