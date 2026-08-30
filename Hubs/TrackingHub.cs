using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using TMPMS.Data;
using TMPMS.Services;

namespace TMPMS.Hubs
{
    // Trước đây hub này không có [Authorize] và không kiểm tra chủ đơn hàng — bất kỳ ai (kể cả chưa
    // đăng nhập) gọi JoinOrderTrackingGroup(orderId) sẽ (1) xem được tên/SĐT/biển số shipper + tọa độ
    // GPS của đơn người khác, và (2) tự kích hoạt TrackingSimulationService ghi thẳng Order.Status vào
    // DB thành "Shipping" rồi "Delivered" dù đơn chưa hề được giao thật.
    [Authorize]
    public class TrackingHub : Hub
    {
        private readonly TrackingSimulationService _simulationService;
        private readonly TMPMSDbContext _context;

        public TrackingHub(TrackingSimulationService simulationService, TMPMSDbContext context)
        {
            _simulationService = simulationService;
            _context = context;
        }

        private int? GetUserId()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : (int?)null;
        }

        private bool CanProxy() =>
            Context.User?.IsInRole("Admin") == true || Context.User?.IsInRole("Staff") == true || Context.User?.IsInRole("Pharmacy") == true;

        public async Task JoinOrderTrackingGroup(string orderId)
        {
            var currentUserId = GetUserId();
            if (currentUserId == null || !int.TryParse(orderId, out int id)) return;

            if (!CanProxy())
            {
                var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
                if (order == null || order.UserId != currentUserId.Value) return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, orderId);
            _simulationService.StartSimulation(id);
        }

        public async Task LeaveOrderTrackingGroup(string orderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, orderId);
        }
    }
}
