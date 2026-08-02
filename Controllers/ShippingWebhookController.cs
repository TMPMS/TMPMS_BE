using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TMPMS.Data;
using TMPMS.Hubs;
using TMPMS.Services;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/shipping")]
    public class ShippingWebhookController : ControllerBase
    {
        private readonly IHubContext<TrackingHub> _hubContext;
        private readonly TrackingSimulationService _simulationService;
        private readonly TMPMSDbContext _context;

        public ShippingWebhookController(IHubContext<TrackingHub> hubContext, TrackingSimulationService simulationService, TMPMSDbContext context)
        {
            _hubContext = hubContext;
            _simulationService = simulationService;
            _context = context;
        }

        public class WebhookRequest
        {
            public int OrderId { get; set; }
            public string Status { get; set; } = "";
            public TrackingSimulationService.Coords? Coords { get; set; }
            public object? Shipper { get; set; }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveWebhook([FromBody] WebhookRequest request)
        {
            if (request.OrderId <= 0 || string.IsNullOrEmpty(request.Status))
            {
                return BadRequest(new { error = "orderId and status are required" });
            }

            // Update simulation service
            _simulationService.UpdateSimulationFromWebhook(request.OrderId, request.Status, request.Coords, request.Shipper);

            // Persist Order.Status (Shipping / Delivered) in the database
            var orderStatus = TrackingStatusSync.ToOrderStatus(request.Status);
            if (orderStatus != null)
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId);
                if (order != null)
                {
                    order.Status = orderStatus;
                    await _context.SaveChangesAsync();
                }
            }

            // Broadcast to SignalR group immediately
            await _hubContext.Clients.Group(request.OrderId.ToString()).SendAsync("ReceiveTrackingUpdate", new
            {
                orderId = request.OrderId,
                status = request.Status,
                shipper = request.Shipper,
                coords = request.Coords
            });

            return Ok(new { success = true, message = "Status updated and broadcasted via SignalR" });
        }
    }
}
