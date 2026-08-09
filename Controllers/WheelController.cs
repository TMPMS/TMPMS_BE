using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMPMS.Data;

namespace TMPMS.Controllers
{
    [ApiController]
    public class WheelController : ControllerBase
    {
        private readonly TMPMSDbContext _context;

        public WheelController(TMPMSDbContext context)
        {
            _context = context;
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : (int?)null;
        }

        // Danh sách mẫu phần thưởng vòng quay — thứ tự cố định theo Id, FE dùng thứ tự này
        // làm chỉ số ô (segment) trên bánh xe.
        private Task<List<Voucher>> GetPrizeTemplatesAsync()
        {
            return _context.Vouchers
                .Where(v => v.IsWheelPrize && v.IsActive)
                .OrderBy(v => v.Id)
                .ToListAsync();
        }

        // GET /vouchers/wheel/prizes
        [HttpGet("vouchers/wheel/prizes")]
        [HttpGet("api/vouchers/wheel/prizes")]
        public async Task<IActionResult> GetPrizes()
        {
            var templates = await GetPrizeTemplatesAsync();
            var result = templates.Select(t => new
            {
                t.Id,
                t.Name,
                t.Type,
                t.DiscountType,
                t.DiscountValue,
                t.MinOrderValue,
                t.MaxDiscount
            });
            return Ok(result);
        }

        // GET /vouchers/wheel/status
        [HttpGet("vouchers/wheel/status")]
        [HttpGet("api/vouchers/wheel/status")]
        [Authorize]
        public async Task<IActionResult> GetStatus()
        {
            var currentUserId = GetUserId();
            if (currentUserId == null) return Unauthorized();

            var today = DateTime.UtcNow.Date;
            var lastSpin = await _context.WheelSpins
                .Include(w => w.Voucher)
                .Where(w => w.UserId == currentUserId)
                .OrderByDescending(w => w.SpinDate)
                .FirstOrDefaultAsync();

            var spunToday = lastSpin != null && lastSpin.SpinDate == today;

            return Ok(new
            {
                canSpinToday = !spunToday,
                nextResetAtUtc = today.AddDays(1),
                lastSpin = lastSpin == null ? null : new
                {
                    lastSpin.SpinDate,
                    voucher = lastSpin.Voucher
                }
            });
        }

        // POST /vouchers/wheel/spin
        [HttpPost("vouchers/wheel/spin")]
        [HttpPost("api/vouchers/wheel/spin")]
        [Authorize]
        public async Task<IActionResult> Spin()
        {
            var currentUserId = GetUserId();
            if (currentUserId == null) return Unauthorized();

            var today = DateTime.UtcNow.Date;
            var alreadySpun = await _context.WheelSpins
                .AnyAsync(w => w.UserId == currentUserId && w.SpinDate == today);
            if (alreadySpun)
            {
                return Conflict(new { error = "Bạn đã quay hôm nay rồi, hẹn ngày mai nhé!" });
            }

            var templates = await GetPrizeTemplatesAsync();
            if (templates.Count == 0)
            {
                return BadRequest(new { error = "Vòng quay hiện chưa có phần thưởng, vui lòng quay lại sau." });
            }

            var totalWeight = templates.Sum(t => t.Weight);
            if (totalWeight <= 0)
            {
                return BadRequest(new { error = "Vòng quay hiện chưa cấu hình đúng, vui lòng liên hệ quản trị viên." });
            }

            var roll = Random.Shared.Next(1, totalWeight + 1);
            var cumulative = 0;
            var prizeIndex = 0;
            var chosen = templates[0];
            for (int i = 0; i < templates.Count; i++)
            {
                cumulative += templates[i].Weight;
                if (roll <= cumulative)
                {
                    chosen = templates[i];
                    prizeIndex = i;
                    break;
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var code = await GenerateUniqueVoucherCodeAsync();
                var won = new Voucher
                {
                    Code = code,
                    Name = chosen.Name,
                    DiscountType = chosen.DiscountType,
                    DiscountValue = chosen.DiscountValue,
                    MinOrderValue = chosen.MinOrderValue,
                    MaxDiscount = chosen.MaxDiscount,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(7),
                    UsageLimit = 1,
                    UsedCount = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Type = chosen.Type,
                    OwnerUserId = currentUserId,
                    IsWheelPrize = false,
                    Weight = 0
                };
                _context.Vouchers.Add(won);
                await _context.SaveChangesAsync();

                var spin = new WheelSpin
                {
                    UserId = currentUserId.Value,
                    SpinDate = today,
                    VoucherId = won.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.WheelSpins.Add(spin);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { voucher = won, prizeIndex });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                // Va chạm unique index (UserId, SpinDate) — 2 request quay cùng lúc.
                return Conflict(new { error = "Bạn đã quay hôm nay rồi, hẹn ngày mai nhé!" });
            }
        }

        private async Task<string> GenerateUniqueVoucherCodeAsync()
        {
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // bỏ 0/O/1/I dễ nhầm lẫn
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var suffix = new string(Enumerable.Range(0, 6)
                    .Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
                var code = $"WHEEL-{suffix}";
                if (!await _context.Vouchers.AnyAsync(v => v.Code == code))
                {
                    return code;
                }
            }
            throw new InvalidOperationException("Không thể tạo mã voucher duy nhất, vui lòng thử lại.");
        }
    }
}
