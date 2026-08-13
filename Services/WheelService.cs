using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using TMPMS.Repositories.Interfaces;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class WheelService : IWheelService
    {
        private readonly IVoucherRepository _repo;
        public WheelService(IVoucherRepository repo) => _repo = repo;

        public async Task<List<WheelPrizeDto>> GetPrizesAsync()
        {
            var templates = await _repo.GetWheelPrizeTemplatesAsync();
            return templates.Select(t => new WheelPrizeDto
            {
                Id = t.Id,
                Name = t.Name,
                Type = t.Type,
                DiscountType = t.DiscountType,
                DiscountValue = t.DiscountValue,
                MinOrderValue = t.MinOrderValue,
                MaxDiscount = t.MaxDiscount
            }).ToList();
        }

        public async Task<WheelStatusResult> GetStatusAsync(int userId, bool isBlockedRole)
        {
            if (isBlockedRole)
            {
                return new WheelStatusResult { CanSpinToday = false, NextResetAtUtc = null, Blocked = true };
            }

            var today = DateTime.UtcNow.Date;
            var lastSpin = await _repo.GetLastSpinAsync(userId);
            var spunToday = lastSpin != null && lastSpin.SpinDate == today;

            return new WheelStatusResult
            {
                CanSpinToday = !spunToday,
                NextResetAtUtc = today.AddDays(1),
                Blocked = false,
                LastSpinDate = lastSpin?.SpinDate,
                LastSpinVoucher = lastSpin?.Voucher
            };
        }

        public async Task<WheelSpinResult> SpinAsync(int userId, bool isBlockedRole)
        {
            if (isBlockedRole)
            {
                return new WheelSpinResult { Success = false, Error = "forbidden" };
            }

            var today = DateTime.UtcNow.Date;
            var alreadySpun = await _repo.HasSpunTodayAsync(userId, today);
            if (alreadySpun)
            {
                return new WheelSpinResult { Success = false, AlreadySpunToday = true, Error = "Bạn đã quay hôm nay rồi, hẹn ngày mai nhé!" };
            }

            var templates = await _repo.GetWheelPrizeTemplatesAsync();
            if (templates.Count == 0)
            {
                return new WheelSpinResult { Success = false, Error = "Vòng quay hiện chưa có phần thưởng, vui lòng quay lại sau." };
            }

            var totalWeight = templates.Sum(t => t.Weight);
            if (totalWeight <= 0)
            {
                return new WheelSpinResult { Success = false, Error = "Vòng quay hiện chưa cấu hình đúng, vui lòng liên hệ quản trị viên." };
            }

            // Quay số có trọng số: roll ngẫu nhiên trong [1, totalWeight], cộng dồn Weight từng mẫu
            // tới khi vượt roll thì chọn mẫu đó.
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

            var tx = await _repo.BeginTransactionAsync();
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
                    OwnerUserId = userId,
                    IsWheelPrize = false,
                    Weight = 0
                };
                await _repo.AddWonVoucherAsync(won);

                var spin = new WheelSpin
                {
                    UserId = userId,
                    SpinDate = today,
                    VoucherId = won.Id,
                    CreatedAt = DateTime.UtcNow
                };
                await _repo.AddSpinAsync(spin);

                await tx.CommitAsync();

                return new WheelSpinResult { Success = true, Voucher = won, PrizeIndex = prizeIndex };
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync();
                // Va chạm unique index (UserId, SpinDate) — 2 request quay cùng lúc.
                return new WheelSpinResult { Success = false, AlreadySpunToday = true, Error = "Bạn đã quay hôm nay rồi, hẹn ngày mai nhé!" };
            }
            finally
            {
                await tx.DisposeAsync();
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
                if (!await _repo.VoucherCodeExistsAsync(code))
                {
                    return code;
                }
            }
            throw new InvalidOperationException("Không thể tạo mã voucher duy nhất, vui lòng thử lại.");
        }
    }
}
