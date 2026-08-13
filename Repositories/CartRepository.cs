using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPMS.Data;
using TMPMS.Repositories.Interfaces;

namespace TMPMS.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly TMPMSDbContext _context;
        public CartRepository(TMPMSDbContext context) => _context = context;

        public async Task<List<Cart>> GetCartsByUserIdAsync(int userId) =>
            await _context.Carts.Where(c => c.UserId == userId).ToListAsync();

        public async Task<Cart?> GetCartByIdAsync(int cartId) =>
            await _context.Carts.FirstOrDefaultAsync(c => c.Id == cartId);

        public async Task<Cart?> GetCartByUserIdSingleAsync(int userId) =>
            await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);

        public async Task<Cart> CreateCartAsync(Cart cart)
        {
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
            return cart;
        }

        public async Task<List<CartItem>> GetCartItemsWithMedicineAsync(int cartId) =>
            await _context.CartItems.Where(ci => ci.CartId == cartId).Include(ci => ci.Medicine).ToListAsync();

        public async Task<CartItem?> GetCartItemByIdAsync(int itemId) =>
            await _context.CartItems.FindAsync(itemId);

        public async Task<CartItem?> FindCartItemAsync(int cartId, int medicineId) =>
            await _context.CartItems.FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.MedicineId == medicineId);

        public async Task<Medicine?> GetMedicineByIdAsync(int medicineId) =>
            await _context.Medicines.FindAsync(medicineId);

        public async Task<CartItem> AddCartItemAsync(CartItem item)
        {
            _context.CartItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task RemoveCartItemAsync(CartItem item)
        {
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task<int> GetPrescribedQuantityAsync(int userId, int medicineId)
        {
            return await _context.PrescriptionItems
                .Where(pi => pi.MedicineId == medicineId && pi.Prescription.UserId == userId)
                .Where(pi => pi.Prescription.Status == "Approved" || pi.Prescription.Status == "Fulfilled")
                .SumAsync(pi => pi.Quantity);
        }

        public async Task<int> GetPurchasedQuantityAsync(int userId, int medicineId)
        {
            return await _context.OrderItems
                .Where(oi => oi.MedicineId == medicineId && oi.Order.UserId == userId && oi.Order.Status != "Cancelled")
                .SumAsync(oi => oi.Quantity);
        }
    }
}
