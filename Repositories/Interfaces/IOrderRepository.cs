using BusinessObjects;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPMS.DTOs;

namespace TMPMS.Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<int> GetDefaultWarehouseIdAsync();
        Task<Medicine?> GetMedicineByIdAsync(int id);
        Task<Order> CreateOrderAsync(Order order);
        void AddOrderItem(OrderItem item);
        void AddPayment(Payment payment);
        Task<Cart?> GetCartByUserIdAsync(int userId);
        Task<List<CartItem>> GetCartItemsByCartIdAsync(int cartId);
        void RemoveCartItems(List<CartItem> items);
        Task SaveChangesAsync();

        Task<List<OrderSummaryDto>> GetUserOrdersAsync(int userId);
        Task<List<OrderSummaryDto>> GetAdminOrdersAsync();

        Task<Order?> GetByIdAsync(int id);
        Task<List<OrderItem>> GetOrderItemsAsync(int orderId);
        Task<Voucher?> GetVoucherByIdAsync(int id);
        Task<Payment?> GetLatestPaymentAsync(int orderId);

        // Đơn còn "Pending"/"Unpaid" quá lâu (khách bỏ ngang, không qua PayOS nên không có webhook tự
        // hủy) — dùng cho StaleOrderBackgroundService để tự hủy + hoàn kho, tránh giữ chỗ FEFO vô thời hạn.
        Task<List<int>> GetStalePendingOrderIdsAsync(System.DateTime olderThan);
    }
}
