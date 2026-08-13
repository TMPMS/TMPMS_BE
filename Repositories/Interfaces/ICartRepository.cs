using BusinessObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TMPMS.Repositories.Interfaces
{
    public interface ICartRepository
    {
        Task<List<Cart>> GetCartsByUserIdAsync(int userId);
        Task<Cart?> GetCartByIdAsync(int cartId);
        Task<Cart?> GetCartByUserIdSingleAsync(int userId);
        Task<Cart> CreateCartAsync(Cart cart);

        Task<List<CartItem>> GetCartItemsWithMedicineAsync(int cartId);
        Task<CartItem?> GetCartItemByIdAsync(int itemId);
        Task<CartItem?> FindCartItemAsync(int cartId, int medicineId);
        Task<Medicine?> GetMedicineByIdAsync(int medicineId);
        Task<CartItem> AddCartItemAsync(CartItem item);
        Task RemoveCartItemAsync(CartItem item);
        Task SaveChangesAsync();

        // Rx Allowance — số lượng đã được kê đơn / đã mua, dùng để tính giới hạn mua thuốc kê đơn.
        Task<int> GetPrescribedQuantityAsync(int userId, int medicineId);
        Task<int> GetPurchasedQuantityAsync(int userId, int medicineId);
    }
}
