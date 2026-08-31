using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using PayOS;
using PayOS.Models;
using PayOS.Models.Webhooks;
using PayOS.Models.V2.PaymentRequests;
using TMPMS.Data;
using System.Security.Claims;
using BusinessObjects;
using TMPMS.Models;
using Services.Interfaces;
using TMPMS.Services.Interfaces;
using TMPMS.Utils;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/payos")]
    [Route("payos")]
    public class PayOSController : ControllerBase
    {
        private readonly TMPMSDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IInventoryService _inventoryService;
        private readonly IAuditLogService _auditLogService;

        public PayOSController(TMPMSDbContext context, IConfiguration configuration, IInventoryService inventoryService, IAuditLogService auditLogService)
        {
            _context = context;
            _configuration = configuration;
            _inventoryService = inventoryService;
            _auditLogService = auditLogService;
        }

        // Khi PayOS báo giao dịch bị hủy/hết hạn, đơn hàng liên quan phải được hủy thật
        // (hoàn kho, hoàn lượt voucher) — không được để đơn kẹt ở "Chờ duyệt" với hàng đã bị trừ kho.
        private async Task CancelUnpaidOrderAsync(Order order)
        {
            if (order.Status != "Pending") return;

            var warehouseId = (await _context.Warehouses.FirstOrDefaultAsync())?.Id ?? 1;
            var items = await _context.OrderItems.Where(oi => oi.OrderId == order.Id).ToListAsync();
            foreach (var oi in items)
            {
                await _inventoryService.RestoreStockFEFO(oi.MedicineId, warehouseId, oi.Quantity, $"ORDER-{order.Id}-RESTOCK", $"ORDER-{order.Id}");
            }

            foreach (var voucherId in new[] { order.ProductVoucherId, order.ShippingVoucherId })
            {
                if (voucherId == null) continue;
                var voucher = await _context.Vouchers.FindAsync(voucherId.Value);
                if (voucher != null) voucher.UsedCount = Math.Max(0, voucher.UsedCount - 1);
            }

            order.Status = "Cancelled";
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
                if (verified.OrderCode >= 800000000000L)
                {
                    var intent = await _context.AppointmentPaymentIntents.Include(i => i.SlotHold).FirstOrDefaultAsync(i => i.OrderCode == verified.OrderCode);
                    if (intent == null) return Ok(new { success = true });
                    if (verified.Code == "00" && verified.Amount == intent.Amount) await FinalizeAppointmentPayment(intent, verified.Reference);
                    return Ok(new { success = true });
                }
                var order = await _context.Orders
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.Id == verified.OrderCode);

                // PayOS gửi một payload mẫu khi xác nhận webhook; vẫn cần trả 2xx.
                if (order == null) return Ok(new { success = true });

                // Idempotency: PayOS có thể gửi lại cùng 1 webhook nhiều lần (retry khi timeout).
                // Nếu đơn đã "Paid" rồi thì không xử lý lại (tránh ghi đè PaidAt/TransactionCode
                // của lần thanh toán gốc bằng dữ liệu của lần gọi lại).
                if (order.PaymentStatus == "Paid") return Ok(new { success = true });

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

                    await this.LogAuditAsync(_auditLogService, "Payment", "WebhookSuccess", order.Id.ToString(), $"PayOS webhook xác nhận thanh toán thành công đơn hàng #{order.Id} — {verified.Amount:N0}đ (ref {verified.Reference})");
                }

                return Ok(new { success = true });
            }
            catch
            {
                return BadRequest(new { error = "Webhook PayOS không hợp lệ." });
            }
        }

        [HttpPost("appointment/verify/{orderCode:long}")]
        [Authorize]
        public async Task<IActionResult> VerifyAppointmentPayment(long orderCode)
        {
            var intent = await _context.AppointmentPaymentIntents.Include(i => i.SlotHold).FirstOrDefaultAsync(i => i.OrderCode == orderCode);
            if (intent == null) return NotFound(new { error = "Không tìm thấy giao dịch đặt cọc." });
            var currentUserId = GetCurrentUserId(); if (currentUserId == null) return Unauthorized();
            if (!CanProxy() && intent.UserId != currentUserId.Value) return Forbid();
            try
            {
                var link = await CreateClient().PaymentRequests.GetAsync(orderCode);
                var status = link.Status.ToString().ToUpperInvariant();
                if (status == "PAID") await FinalizeAppointmentPayment(intent, orderCode.ToString());
                else if (status is "CANCELLED" or "EXPIRED") { intent.Status = status == "EXPIRED" ? "Expired" : "Cancelled"; intent.SlotHold.IsConsumed = true; await _context.SaveChangesAsync(); }
                var appointmentId = await _context.AppointmentPayments.Where(p => p.TransactionCode == orderCode.ToString()).Select(p => (int?)p.AppointmentId).FirstOrDefaultAsync();
                return Ok(new { orderCode, status, intentStatus = intent.Status, appointmentId });
            }
            catch (Exception ex) { return BadRequest(new { error = $"Không thể kiểm tra giao dịch PayOS: {ex.Message}" }); }
        }

        private async Task FinalizeAppointmentPayment(AppointmentPaymentIntent intent, string transactionCode)
        {
            if (intent.Status == "Paid") return;
            if (intent.ExpiresAt < DateTime.UtcNow) { intent.Status = "Expired"; intent.SlotHold.IsConsumed = true; await _context.SaveChangesAsync(); return; }
            await using var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var occupied = await _context.Appointments.AnyAsync(a => a.Location == intent.SlotHold.Location && a.AppointmentDate == intent.SlotHold.AppointmentDate
                && new[] { "PendingConfirmation", "Confirmed", "CheckedIn", "AlternativeProposed" }.Contains(a.Status));
            if (occupied) { intent.Status = "ManualReview"; await _context.SaveChangesAsync(); await tx.CommitAsync(); return; }
            var appointment = new Appointment
            {
                UserId = intent.UserId, AppointmentDate = intent.SlotHold.AppointmentDate, Location = intent.SlotHold.Location,
                SymptomDescription = intent.SymptomDescription, Reason = intent.SymptomDescription, Note = intent.Note,
                PrescriptionImageUrl = intent.PrescriptionImageUrl, DepositAmount = intent.Amount, PaymentMethod = "PayOS",
                PaymentStatus = "Paid", Status = "PendingConfirmation", PolicyAcceptedAt = intent.CreatedAt,
                CreatedAt = DateTime.UtcNow, ConfirmationDeadline = DateTime.UtcNow.AddHours(24)
            };
            _context.Appointments.Add(appointment); await _context.SaveChangesAsync();
            _context.AppointmentPayments.Add(new AppointmentPayment { AppointmentId = appointment.Id, Amount = intent.Amount, Method = "PayOS", Status = "Paid", TransactionCode = transactionCode, CreatedAt = DateTime.UtcNow, PaidAt = DateTime.UtcNow });
            intent.Status = "Paid"; intent.SlotHold.IsConsumed = true; await _context.SaveChangesAsync(); await tx.CommitAsync();

            var isDemo = transactionCode.StartsWith("DEMO-", StringComparison.OrdinalIgnoreCase);
            await this.LogAuditAsync(_auditLogService, "Payment", isDemo ? "DemoPayAppointment" : "AppointmentPaymentSuccess", appointment.Id.ToString(), $"Đặt cọc lịch hẹn #{appointment.Id} thành công ({intent.Amount:N0}đ, mã GD {transactionCode}){(isDemo ? " — GIẢ LẬP (demo-pay)" : "")}");
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
                    await this.LogAuditAsync(_auditLogService, "Payment", "VerifySuccess", order.Id.ToString(), $"Xác minh thanh toán PayOS thành công cho đơn hàng #{order.Id}");
                }
                else if (payOSStatus is "CANCELLED" or "EXPIRED")
                {
                    order.PaymentStatus = "Failed";
                    if (payment != null) payment.Status = "Failed";
                    await CancelUnpaidOrderAsync(order);
                    await this.LogAuditAsync(_auditLogService, "Payment", "VerifyFailed", order.Id.ToString(), $"Giao dịch PayOS đơn hàng #{order.Id} bị {payOSStatus} — đơn tự động hủy, hoàn kho");
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

        // Demo thanh toán tiền cọc lịch hẹn — giống demo-pay/{orderId} cho đơn hàng, bỏ qua PayOS thật
        // để demo/test luồng đặt lịch nhanh, không cần quét VietQR.
        [HttpPost("appointment/demo-pay/{orderCode:long}")]
        [Authorize]
        public async Task<IActionResult> DemoPayAppointment(long orderCode)
        {
            var intent = await _context.AppointmentPaymentIntents.Include(i => i.SlotHold).FirstOrDefaultAsync(i => i.OrderCode == orderCode);
            if (intent == null) return NotFound(new { error = "Không tìm thấy giao dịch đặt cọc." });

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();
            if (!CanProxy() && intent.UserId != currentUserId.Value) return Forbid();

            var demoTransactionCode = "DEMO-PAYOS-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await FinalizeAppointmentPayment(intent, demoTransactionCode);

            var appointmentId = await _context.AppointmentPayments
                .Where(p => p.TransactionCode == demoTransactionCode)
                .Select(p => (int?)p.AppointmentId)
                .FirstOrDefaultAsync();

            return Ok(new { success = true, orderCode, intentStatus = intent.Status, appointmentId, message = "Giả lập thanh toán PayOS thành công cho tiền cọc lịch hẹn." });
        }

        [HttpPost("demo-pay/{orderId:int}")]
        [Authorize]
        public async Task<IActionResult> DemoPay(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound(new { error = "Không tìm thấy đơn hàng." });

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();
            if (!CanProxy() && order.UserId != currentUserId.Value) return Forbid();

            order.PaymentStatus = "Paid";
            var payment = order.Payments.FirstOrDefault();
            if (payment != null)
            {
                payment.Method = "PayOS (Demo)";
                payment.Status = "Success";
                payment.PaidAt = DateTime.UtcNow;
                payment.TransactionCode = "DEMO-PAYOS-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            else
            {
                order.Payments.Add(new Payment
                {
                    OrderId = order.Id,
                    Method = "PayOS (Demo)",
                    Amount = order.TotalAmount,
                    Status = "Success",
                    TransactionCode = "DEMO-PAYOS-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    PaidAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            await this.LogAuditAsync(_auditLogService, "Payment", "DemoPay", order.Id.ToString(), $"Giả lập thanh toán PayOS thành công cho đơn hàng #{order.Id} ({order.TotalAmount:N0}đ) — GIẢ LẬP (demo-pay)");

            return Ok(new
            {
                success = true,
                orderId = order.Id,
                paymentStatus = "Paid",
                message = "Giả lập thanh toán PayOS thành công cho đơn hàng #" + order.Id
            });
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
