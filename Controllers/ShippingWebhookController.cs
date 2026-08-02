using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
        public const string SignatureHeader = "X-Signature";

        private readonly IHubContext<TrackingHub> _hubContext;
        private readonly TrackingSimulationService _simulationService;
        private readonly TMPMSDbContext _context;
        private readonly string _webhookSecret;

        public ShippingWebhookController(
            IHubContext<TrackingHub> hubContext,
            TrackingSimulationService simulationService,
            TMPMSDbContext context,
            IConfiguration configuration)
        {
            _hubContext = hubContext;
            _simulationService = simulationService;
            _context = context;
            _webhookSecret = configuration["Shipping:WebhookSecret"] ?? string.Empty;
        }

        public class WebhookRequest
        {
            public int OrderId { get; set; }
            public string Status { get; set; } = "";
            public TrackingSimulationService.Coords? Coords { get; set; }
            public object? Shipper { get; set; }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveWebhook()
        {
            var rawBody = await ReadRawBodyAsync();

            // Verify HMAC-SHA256 signature (header X-Signature = lowercase hex of
            // HMAC-SHA256(webhookSecret, raw request body)). Fail closed: no secret
            // configured or no header => reject.
            if (!VerifySignature(rawBody))
            {
                return Unauthorized(new { error = "Invalid signature" });
            }

            WebhookRequest request;
            try
            {
                request = JsonSerializer.Deserialize<WebhookRequest>(rawBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new WebhookRequest();
            }
            catch (JsonException)
            {
                return BadRequest(new { error = "Invalid JSON payload" });
            }

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

        private async Task<string> ReadRawBodyAsync()
        {
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            Request.Body.Position = 0;
            return body;
        }

        private bool VerifySignature(string rawBody)
        {
            if (string.IsNullOrEmpty(_webhookSecret))
            {
                return false;
            }

            if (!Request.Headers.TryGetValue(SignatureHeader, out var provided))
            {
                return false;
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
            var expected = Convert.ToHexString(hash).ToLowerInvariant();

            return FixedTimeEquals(expected, provided.ToString());
        }

        private static bool FixedTimeEquals(string expected, string provided)
        {
            if (expected.Length != provided.Length)
            {
                return false;
            }
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(provided));
        }
    }
}
