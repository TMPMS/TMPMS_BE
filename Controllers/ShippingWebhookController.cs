using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
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

        public ShippingWebhookController(IHubContext<TrackingHub> hubContext, TrackingSimulationService simulationService)
        {
            _hubContext = hubContext;
            _simulationService = simulationService;
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
