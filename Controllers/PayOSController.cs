using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models;
using PayOS.Models.Webhooks;
using PayOS.Models.V2.PaymentRequests;
using TMPMS.Data;
using System.Security.Claims;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/payos")]
    public class PayOSController : ControllerBase
    {
        private readonly TMPMSDbContext _context;
        private readonly IConfiguration _configuration;

        public PayOSController(TMPMSDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public class CreatePaymentLinkInput
        {
            public int OrderId { get; set; }
            public string ReturnUrl { get; set; } = string.Empty;
            public string CancelUrl { get; set; } = string.Empty;
        }

        private PayOSClient CreateClient()
        {
            var clientId = _configuration["PayOS:ClientId"];
            var apiKey = _configuration["PayOS:ApiKey"];
            var checksumKey = _configuration["PayOS:ChecksumKey"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(checksumKey))
            {
                throw new InvalidOperationException("PayOS chưa được cấu hình ClientId, ApiKey và ChecksumKey.");
            }

            return new PayOSClient(clientId, apiKey, checksumKey);
        }

        [HttpPost("payment-link")]
        [Authorize]
        public async Task<IActionResult> CreatePaymentLink([FromBody] CreatePaymentLinkInput input)
        {
            if (!Uri.TryCreate(input.ReturnUrl, UriKind.Absolute, out _) ||
                !Uri.TryCreate(input.CancelUrl, UriKind.Absolute, out _))
            {
                return BadRequest(new { error = "ReturnUrl hoặc CancelUrl không hợp lệ." });
            }

            var order = await _context.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == input.OrderId);

            if (order == null) return NotFound(new { error = "Không tìm thấy đơn hàng." });

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();
            if (!CanProxy() && order.UserId != currentUserId.Value) return Forbid();

            if (order.PaymentStatus == "Paid") return BadRequest(new { error = "Đơn hàng đã được thanh toán." });
            if (order.TotalAmount <= 0 || order.TotalAmount > int.MaxValue)
                return BadRequest(new { error = "Số tiền thanh toán không hợp lệ." });

            var payment = order.Payments.FirstOrDefault();
            if (payment == null)
            {
                return BadRequest(new { error = "Đơn hàng chưa có bản ghi thanh toán." });
            }

            payment.Method = "PayOS";
            payment.Status = "Pending";
            payment.TransactionCode = order.Id.ToString();
            await _context.SaveChangesAsync();

            try
            {
                var request = new CreatePaymentLinkRequest
                {
                    OrderCode = order.Id,
                    Amount = decimal.ToInt32(decimal.Round(order.TotalAmount, 0)),
                    Description = $"DON HANG {order.Id}",
                    ReturnUrl = input.ReturnUrl,
                    CancelUrl = input.CancelUrl
                };

                var link = await CreateClient().PaymentRequests.CreateAsync(request);
                return Ok(new
                {
                    checkoutUrl = link.CheckoutUrl,
                    paymentLinkId = link.PaymentLinkId,
                    orderCode = link.OrderCode
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Không thể tạo link PayOS: {ex.Message}" });
            }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] Webhook webhookData)
        {
            try
            {
                var verified = await CreateClient().Webhooks.VerifyAsync(webhookData);
                var order = await _context.Orders
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.Id == verified.OrderCode);

                // PayOS gửi một payload mẫu khi xác nhận webhook; vẫn cần trả 2xx.
                if (order == null) return Ok(new { success = true });

                var payment = order.Payments.FirstOrDefault(p => p.Method == "PayOS")
                    ?? order.Payments.FirstOrDefault();

                if (payment != null && verified.Code == "00" && verified.Amount == order.TotalAmount)
                {
                    payment.Method = "PayOS";
                    payment.Status = "Success";
                    payment.TransactionCode = verified.Reference;
                    payment.PaidAt = DateTime.UtcNow;
                    order.PaymentStatus = "Paid";
                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true });
            }
            catch
            {
                return BadRequest(new { error = "Webhook PayOS không hợp lệ." });
            }
        }

        [HttpPost("verify/{orderId:int}")]
        [Authorize]
        public async Task<IActionResult> VerifyPayment(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound(new { error = "Không tìm thấy đơn hàng." });

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();
            if (!CanProxy() && order.UserId != currentUserId.Value) return Forbid();

            try
            {
                // Không tin dữ liệu trên return URL; luôn hỏi lại PayOS bằng API server-to-server.
                var paymentLink = await CreateClient().PaymentRequests.GetAsync((long)orderId);
                var payOSStatus = paymentLink.Status.ToString().ToUpperInvariant();
                var payment = order.Payments.FirstOrDefault(p => p.Method == "PayOS")
                    ?? order.Payments.FirstOrDefault();

                if (payOSStatus == "PAID")
                {
                    order.PaymentStatus = "Paid";
                    if (payment != null)
                    {
                        payment.Method = "PayOS";
                        payment.Status = "Success";
                        payment.PaidAt ??= DateTime.UtcNow;
                    }
                }
                else if (payOSStatus is "CANCELLED" or "EXPIRED")
                {
                    order.PaymentStatus = "Failed";
                    if (payment != null) payment.Status = "Failed";
                }

                await _context.SaveChangesAsync();
                return Ok(new
                {
                    orderId = order.Id,
                    status = payOSStatus,
                    paymentStatus = order.PaymentStatus
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Không thể kiểm tra giao dịch PayOS: {ex.Message}" });
            }
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : (int?)null;
        }

        private bool CanProxy()
        {
            return User.IsInRole("Admin") || User.IsInRole("Staff") || User.IsInRole("Pharmacy");
        }
    }
}
