using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using TMPMS.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TMPMS.Controllers
{
    [ApiController]
    public class VouchersController : ControllerBase
    {
        private readonly TMPMSDbContext _context;

        public VouchersController(TMPMSDbContext context)
        {
            _context = context;
        }

        // GET /vouchers
        [HttpGet("vouchers")]
        public async Task<IActionResult> GetVouchers()
        {
            var vouchers = await _context.Vouchers
                .Where(v => v.IsActive && (v.EndDate == null || v.EndDate > DateTime.UtcNow))
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
            return Ok(vouchers);
        }

        // GET /admin/vouchers
        [HttpGet("admin/vouchers")]
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
        }

        // POST /admin/vouchers
        [HttpPost("admin/vouchers")]
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
                CreatedAt = DateTime.UtcNow
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();
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
        }

        // PATCH /admin/vouchers/{id}
        [HttpPatch("admin/vouchers/{id}")]
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

            await _context.SaveChangesAsync();
            return Ok(voucher);
        }

        // DELETE /admin/vouchers/{id}
        [HttpDelete("admin/vouchers/{id}")]
        public async Task<IActionResult> DeleteVoucher(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound("Voucher not found");

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
            return Ok(voucher);
        }

        public class ValidateVoucherRequest
        {
            public string Code { get; set; } = "";
            public decimal Order_Total { get; set; }
        }

        // POST /vouchers/validate
        [HttpPost("vouchers/validate")]
        public async Task<IActionResult> ValidateVoucher([FromBody] ValidateVoucherRequest request)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Code == request.Code && v.IsActive && (v.EndDate == null || v.EndDate > DateTime.UtcNow) && v.UsedCount < v.UsageLimit);

            if (voucher == null)
            {
                return NotFound(new { error = "Mã voucher không hợp lệ hoặc đã hết hạn" });
            }

            if (request.Order_Total < voucher.MinOrderValue)
            {
                return BadRequest(new { error = $"Đơn hàng tối thiểu {voucher.MinOrderValue:N0}đ để dùng voucher này" });
            }

            decimal discount = 0;
            if (voucher.DiscountType == "percent")
            {
                discount = request.Order_Total * voucher.DiscountValue / 100;
                if (voucher.MaxDiscount.HasValue)
                {
                    discount = Math.Min(discount, voucher.MaxDiscount.Value);
                }
            }
            else
            {
                discount = voucher.DiscountValue;
            }

            return Ok(new { valid = true, voucher = voucher, discount = discount });
        }
    }
}
