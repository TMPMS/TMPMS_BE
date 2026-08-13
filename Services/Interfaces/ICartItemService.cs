using BusinessObjects;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPMS.DTOs;

namespace TMPMS.Services.Interfaces
{
    public class CartSyncItemInput
    {
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
    }

    public class CartSyncResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public bool RequiresPrescription { get; set; }
        public Cart? Cart { get; set; }
    }

    public interface ICartItemService
    {
        Task<Cart?> GetOwnedCartAsync(int cartId, int currentUserId, bool canProxy);
        Task<List<CartItemViewDto>> GetItemsAsync(Cart cart);
        Task<CartItem?> GetItemByIdAsync(int itemId);
        Task<CartItemActionResult> AddItemAsync(Cart cart, int medicineId, int quantity);
        Task<CartItemActionResult> UpdateItemAsync(Cart cart, CartItem item, int quantity);
        Task RemoveItemAsync(CartItem item);
        Task<CartSyncResult> SyncAsync(int userId, List<CartSyncItemInput> items);
    }
}
