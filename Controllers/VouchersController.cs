using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;
using TMPMS.Utils;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TMPMS.Controllers
{
    [ApiController]
    public class VouchersController : ControllerBase
    {
        private readonly IVoucherService _service;
        private readonly IAuditLogService _auditLogService;

        public VouchersController(IVoucherService service, IAuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : (int?)null;
        }

        // GET /vouchers — danh sách voucher công khai (không gồm voucher cá nhân/mẫu vòng quay)
        [HttpGet("vouchers")]
        [HttpGet("api/vouchers")]
        public async Task<IActionResult> GetVouchers()
        {
            var vouchers = await _service.GetPublicVouchersAsync();
            return Ok(vouchers);
        }

        // GET /vouchers/mine — voucher cá nhân của user hiện tại (VD trúng từ vòng quay may mắn)
        [HttpGet("vouchers/mine")]
        [HttpGet("api/vouchers/mine")]
        [Authorize]
        public async Task<IActionResult> GetMyVouchers()
        {
            var currentUserId = GetUserId();
            if (currentUserId == null) return Unauthorized();

            var vouchers = await _service.GetMyVouchersAsync(currentUserId.Value);
            return Ok(vouchers);
        }

        // GET /admin/vouchers
        [HttpGet("admin/vouchers")]
        [HttpGet("api/admin/vouchers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminVouchers()
        {
            var vouchers = await _service.GetAllAsync();
            return Ok(vouchers);
        }

        // POST /admin/vouchers
        [HttpPost("admin/vouchers")]
        [HttpPost("api/admin/vouchers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateVoucher([FromBody] VoucherCreateInputDto input)
        {
            var voucher = await _service.CreateAsync(input);
            await this.LogAuditAsync(_auditLogService, "Voucher", "Create", voucher.Id.ToString(), $"Tạo voucher '{voucher.Code}' ({voucher.Name})");
            return StatusCode(201, voucher);
        }

        // PATCH /admin/vouchers/{id}
        [HttpPatch("admin/vouchers/{id}")]
        [HttpPatch("api/admin/vouchers/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateVoucher(int id, [FromBody] VoucherUpdateInputDto input)
        {
            var voucher = await _service.UpdateAsync(id, input);
            if (voucher == null) return NotFound("Voucher not found");

            await this.LogAuditAsync(_auditLogService, "Voucher", "Update", id.ToString(), $"Cập nhật voucher #{id} ('{voucher.Code}')");
            return Ok(voucher);
        }

        // DELETE /admin/vouchers/{id}
        [HttpDelete("admin/vouchers/{id}")]
        [HttpDelete("api/admin/vouchers/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVoucher(int id)
        {
            var voucher = await _service.DeleteAsync(id);
            if (voucher == null) return NotFound("Voucher not found");

            await this.LogAuditAsync(_auditLogService, "Voucher", "Delete", id.ToString(), $"Xóa voucher #{id} ('{voucher.Code}')");
            return Ok(voucher);
        }

        // POST /vouchers/validate — preview mức giảm cho 1 mã, dùng chung logic với lúc checkout thật.
        [HttpPost("vouchers/validate")]
        [HttpPost("api/vouchers/validate")]
        [Authorize]
        public async Task<IActionResult> ValidateVoucher([FromBody] ValidateVoucherRequestDto request)
        {
            var currentUserId = GetUserId();
            if (currentUserId == null) return Unauthorized();

            var result = await _service.ValidateAsync(request, currentUserId.Value);
            if (!result.Valid)
            {
                return result.NotFound
                    ? NotFound(new { error = result.Error })
                    : BadRequest(new { error = result.Error });
            }

            return Ok(new { valid = true, voucher = result.Voucher, discount = result.Discount });
        }
    }
}
