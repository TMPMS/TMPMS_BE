using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using TMPMS.Data;
using TMPMS.Services;
using TMPMS.Services.Interfaces;
using TMPMS.Utils;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TMPMS.Controllers
{
    [ApiController]
    public class VouchersController : ControllerBase
    {
        private readonly TMPMSDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public VouchersController(TMPMSDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
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
            var vouchers = await _context.Vouchers
                .Where(v => v.IsActive && !v.IsWheelPrize && v.OwnerUserId == null &&
                    (v.EndDate == null || v.EndDate > DateTime.UtcNow))
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
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

            var vouchers = await _context.Vouchers
                .Where(v => v.OwnerUserId == currentUserId && v.IsActive &&
                    v.UsedCount < v.UsageLimit &&
                    (v.EndDate == null || v.EndDate > DateTime.UtcNow))
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
            return Ok(vouchers);
        }

        // GET /admin/vouchers
        [HttpGet("admin/vouchers")]
        [HttpGet("api/admin/vouchers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminVouchers()
        {
            var vouchers = await _context.Vouchers
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
            return Ok(vouchers);
        }

        public class CreateVoucherInput
        {
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public string DiscountType { get; set; } = "percent";
            public decimal DiscountValue { get; set; }
            public decimal MinOrderValue { get; set; }
            public decimal? MaxDiscount { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public int UsageLimit { get; set; } = 100;
            public bool IsActive { get; set; } = true;
            public string Type { get; set; } = "product";
            public bool IsWheelPrize { get; set; } = false;
            public int Weight { get; set; } = 0;
        }

        // POST /admin/vouchers
        [HttpPost("admin/vouchers")]
        [HttpPost("api/admin/vouchers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateVoucher([FromBody] CreateVoucherInput input)
        {
            var voucher = new Voucher
            {
                Code = input.Code,
                Name = input.Name,
                DiscountType = input.DiscountType,
                DiscountValue = input.DiscountValue,
                MinOrderValue = input.MinOrderValue,
                MaxDiscount = input.MaxDiscount,
                StartDate = input.StartDate ?? DateTime.UtcNow,
                EndDate = input.EndDate,
                UsageLimit = input.UsageLimit,
                IsActive = input.IsActive,
                CreatedAt = DateTime.UtcNow,
                Type = input.Type,
                IsWheelPrize = input.IsWheelPrize,
                Weight = input.Weight,
                // OwnerUserId luôn do server quản lý (voucher cá nhân chỉ được tạo qua vòng quay),
                // không nhận từ input của Admin.
                OwnerUserId = null
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();
            await this.LogAuditAsync(_auditLogService, "Voucher", "Create", voucher.Id.ToString(), $"Tạo voucher '{voucher.Code}' ({voucher.Name})");
            return StatusCode(201, voucher);
        }

        public class UpdateVoucherInput
        {
            public string? Code { get; set; }
            public string? Name { get; set; }
            public string? DiscountType { get; set; }
            public decimal? DiscountValue { get; set; }
            public decimal? MinOrderValue { get; set; }
            public decimal? MaxDiscount { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public int? UsageLimit { get; set; }
            public bool? IsActive { get; set; }
            public string? Type { get; set; }
            public bool? IsWheelPrize { get; set; }
            public int? Weight { get; set; }
        }

        // PATCH /admin/vouchers/{id}
        [HttpPatch("admin/vouchers/{id}")]
        [HttpPatch("api/admin/vouchers/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateVoucher(int id, [FromBody] UpdateVoucherInput input)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound("Voucher not found");

            if (input.Code != null) voucher.Code = input.Code;
            if (input.Name != null) voucher.Name = input.Name;
            if (input.DiscountType != null) voucher.DiscountType = input.DiscountType;
            if (input.DiscountValue != null) voucher.DiscountValue = input.DiscountValue.Value;
            if (input.MinOrderValue != null) voucher.MinOrderValue = input.MinOrderValue.Value;
            if (input.MaxDiscount != null) voucher.MaxDiscount = input.MaxDiscount;
            if (input.StartDate != null) voucher.StartDate = input.StartDate.Value;
            if (input.EndDate != null) voucher.EndDate = input.EndDate;
            if (input.UsageLimit != null) voucher.UsageLimit = input.UsageLimit.Value;
            if (input.IsActive != null) voucher.IsActive = input.IsActive.Value;
            if (input.Type != null) voucher.Type = input.Type;
            if (input.IsWheelPrize != null) voucher.IsWheelPrize = input.IsWheelPrize.Value;
            if (input.Weight != null) voucher.Weight = input.Weight.Value;

            await _context.SaveChangesAsync();
            await this.LogAuditAsync(_auditLogService, "Voucher", "Update", id.ToString(), $"Cập nhật voucher #{id} ('{voucher.Code}')");
            return Ok(voucher);
        }

        // DELETE /admin/vouchers/{id}
        [HttpDelete("admin/vouchers/{id}")]
        [HttpDelete("api/admin/vouchers/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVoucher(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound("Voucher not found");

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
            await this.LogAuditAsync(_auditLogService, "Voucher", "Delete", id.ToString(), $"Xóa voucher #{id} ('{voucher.Code}')");
            return Ok(voucher);
        }

        public class ValidateVoucherRequest
        {
            public string Code { get; set; } = "";
            public decimal Order_Total { get; set; }
            public string Type { get; set; } = "product";
            // Chỉ dùng khi Type == "shipping", để cap số tiền giảm không vượt phí ship thực tế.
            public decimal? ShippingFee { get; set; }
        }

        // POST /vouchers/validate — preview mức giảm cho 1 mã, dùng chung logic với lúc checkout thật.
        [HttpPost("vouchers/validate")]
        [HttpPost("api/vouchers/validate")]
        [Authorize]
        public async Task<IActionResult> ValidateVoucher([FromBody] ValidateVoucherRequest request)
        {
            var currentUserId = GetUserId();
            if (currentUserId == null) return Unauthorized();

            var result = await VoucherResolver.ResolveAsync(_context, request.Code, request.Type, currentUserId);
            if (result.Voucher == null)
            {
                return NotFound(new { error = result.Error ?? "Mã voucher không hợp lệ hoặc đã hết hạn" });
            }

            var voucher = result.Voucher;
            if (request.Order_Total < voucher.MinOrderValue)
            {
                return BadRequest(new { error = $"Đơn hàng tối thiểu {voucher.MinOrderValue:N0}đ để dùng voucher này" });
            }

            var baseAmount = request.Type == "shipping" ? (request.ShippingFee ?? request.Order_Total) : request.Order_Total;
            var discount = VoucherResolver.ComputeDiscount(voucher, baseAmount);

            return Ok(new { valid = true, voucher = voucher, discount = discount });
        }
    }
}
