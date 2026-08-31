using BusinessObjects;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPMS.DTOs;

namespace TMPMS.Services.Interfaces
{
    public enum OrderErrorType { None, NotFound, BadRequest, Conflict, ServerError, Forbidden }

    public class OrderActionResult
    {
        public bool Success { get; set; }
        public Order? Order { get; set; }
        public string? Error { get; set; }
        public OrderErrorType ErrorType { get; set; }
        public string? PreviousStatus { get; set; }
    }

    public interface IOrderService
    {
        Task<OrderActionResult> CreateOrderAsync(CheckoutRequestDto request, int? currentUserId);
        Task<List<OrderSummaryDto>> GetUserOrdersAsync(int userId);
        Task<List<OrderSummaryDto>> GetAdminOrdersAsync();
        Task<Order?> GetByIdAsync(int id);
        Task<OrderActionResult> CancelOrderAsync(int id, int currentUserId, bool canProxy);
        Task<OrderActionResult> RequestReturnAsync(int id, string reason, int currentUserId, bool canProxy);
        Task<OrderActionResult> UpdateStatusAsync(int id, UpdateOrderStatusRequestDto request, int? actingUserId);

        // Tự động hủy + hoàn kho các đơn còn "Pending"/"Unpaid" quá `staleAfter` — dùng bởi
        // StaleOrderBackgroundService, vì các đơn không thanh toán qua PayOS (vd COD bị bỏ quên) không có
        // webhook nào tự hủy chúng, khiến hàng bị giữ chỗ FEFO vô thời hạn. Trả về số đơn đã hủy.
        Task<int> AutoCancelStaleOrdersAsync(System.TimeSpan staleAfter);
    }
}
