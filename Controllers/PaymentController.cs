using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;
using TMPMS.Utils;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _service;
        private readonly IAuditLogService _auditLogService;
        public PaymentController(IPaymentService service, IAuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : (int?)null;
        }

        // Khớp đúng bộ role đã dùng ở UpdateStatus bên dưới (Admin/Accountant/Pharmacy) — các role này
        // được xem/tạo payment thay mặt khách hàng, người dùng thường chỉ được thao tác trên đơn của mình.
        private bool CanProxy() => User.IsInRole("Admin") || User.IsInRole("Accountant") || User.IsInRole("Pharmacy");

        [HttpPost]
        public async Task<ActionResult> CreatePayment([FromBody] PaymentCreateDTO dto)
        {
            var currentUserId = GetUserId();
            if (currentUserId == null) return Unauthorized();
            try
            {
                var result = await _service.CreatePayment(dto, currentUserId.Value, CanProxy());
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var currentUserId = GetUserId();
            if (currentUserId == null) return Unauthorized();
            try
            {
                var result = await _service.GetById(id, currentUserId.Value, CanProxy());
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpGet("order/{orderId}")]
        public async Task<ActionResult> GetByOrder(int orderId)
        {
            var currentUserId = GetUserId();
            if (currentUserId == null) return Unauthorized();
            try
            {
                return Ok(await _service.GetByOrder(orderId, currentUserId.Value, CanProxy()));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Accountant,Pharmacy")]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] PaymentUpdateStatusDTO dto)
        {
            try
            {
                var result = await _service.UpdateStatus(id, dto);
                if (result == null) return NotFound();
                await this.LogAuditAsync(_auditLogService, "Payment", "UpdateStatus", id.ToString(), $"Cập nhật trạng thái thanh toán #{id} → {dto.Status} ({result.Amount:N0}đ, đơn hàng #{result.OrderId})");
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}
