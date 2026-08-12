using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using TMPMS.Data;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class LoyaltyService : ILoyaltyService
    {
        private readonly TMPMSDbContext _context;
        private const decimal VndPerEarnPoint = 10_000m; // Chi 10.000đ đơn hàng = 1 điểm
        private const decimal VndPerRedeemPoint = 1_000m; // 1 điểm đổi được 1.000đ giảm giá
        private const int MinRedeemPoints = 50;

        public LoyaltyService(TMPMSDbContext context) => _context = context;

        public async Task<LoyaltySummaryDto> GetSummaryAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            var transactions = await _context.LoyaltyPointTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .Select(t => new LoyaltyTransactionDto
                {
                    Id = t.Id,
                    Points = t.Points,
                    Reason = t.Reason,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return new LoyaltySummaryDto { Points = user?.LoyaltyPoints ?? 0, Transactions = transactions };
        }

        public async Task<RedeemPointsResultDto> RedeemAsync(int userId, int points)
        {
            if (points < MinRedeemPoints)
                throw new InvalidOperationException($"Cần đổi tối thiểu {MinRedeemPoints} điểm.");

            // Trừ điểm bằng 1 câu UPDATE có điều kiện thay vì "đọc số dư rồi ghi lại" — tránh race
            // condition khi 2 request đổi điểm cùng lúc (2 tab/2 thiết bị) đều đọc thấy đủ số dư
            // trước khi request nào kịp ghi, dẫn tới đổi được nhiều voucher hơn số điểm thực có.
            var rowsAffected = await _context.Users
                .Where(u => u.Id == userId && u.LoyaltyPoints >= points)
                .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.LoyaltyPoints, u => u.LoyaltyPoints - points));

            if (rowsAffected == 0)
            {
                var exists = await _context.Users.AnyAsync(u => u.Id == userId);
                throw new InvalidOperationException(exists ? "Số điểm không đủ để đổi." : "Không tìm thấy tài khoản.");
            }

            var voucher = new Voucher
            {
                Code = $"DIEM{userId}{DateTime.UtcNow:MMddHHmmss}",
                Name = $"Voucher đổi từ {points} điểm tích lũy",
                DiscountType = "flat",
                DiscountValue = points * VndPerRedeemPoint,
                MinOrderValue = 0,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                UsageLimit = 1,
                IsActive = true,
                Type = "product",
                OwnerUserId = userId
            };
            _context.Vouchers.Add(voucher);

            _context.LoyaltyPointTransactions.Add(new LoyaltyPointTransaction
            {
                UserId = userId,
                Points = -points,
                Reason = $"Đổi {points} điểm lấy voucher {voucher.Code}",
                Voucher = voucher
            });

            await _context.SaveChangesAsync();

            // ExecuteUpdateAsync đã trừ điểm thẳng trong DB, không qua change tracker — đọc lại
            // số dư mới nhất (no-tracking) để trả về, vì entity user (nếu có) không được cập nhật.
            var remaining = await _context.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.LoyaltyPoints)
                .FirstAsync();

            return new RedeemPointsResultDto
            {
                RemainingPoints = remaining,
                VoucherCode = voucher.Code,
                DiscountValue = voucher.DiscountValue,
                ExpiresAt = voucher.EndDate
            };
        }

        public async Task AwardForOrderAsync(int orderId)
        {
            // Không cộng điểm 2 lần cho cùng 1 đơn (vd đơn Delivered -> ReturnRequested -> Delivered lại).
            var alreadyAwarded = await _context.LoyaltyPointTransactions
                .AnyAsync(t => t.OrderId == orderId && t.Points > 0);
            if (alreadyAwarded) return;

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return;

            var points = (int)Math.Floor(order.TotalAmount / VndPerEarnPoint);
            if (points <= 0) return;

            var user = await _context.Users.FindAsync(order.UserId);
            if (user == null) return;

            user.LoyaltyPoints += points;
            _context.LoyaltyPointTransactions.Add(new LoyaltyPointTransaction
            {
                UserId = order.UserId,
                Points = points,
                Reason = $"Tích điểm đơn hàng #{orderId}",
                OrderId = orderId
            });

            await _context.SaveChangesAsync();
        }

        public async Task ReverseForReturnedOrderAsync(int orderId)
        {
            var earnTx = await _context.LoyaltyPointTransactions
                .FirstOrDefaultAsync(t => t.OrderId == orderId && t.Points > 0);
            if (earnTx == null) return;

            var alreadyReversed = await _context.LoyaltyPointTransactions
                .AnyAsync(t => t.OrderId == orderId && t.Points < 0);
            if (alreadyReversed) return;

            var user = await _context.Users.FindAsync(earnTx.UserId);
            if (user == null) return;

            // Trừ đủ số điểm đã cộng cho đơn này, kể cả khi khách đã tiêu bớt (đổi voucher) sang
            // việc khác — số dư được phép âm (thành "nợ điểm"), khấu trừ dần vào lần tích điểm kế
            // tiếp. Trước đây giới hạn ở Math.Min(earnTx.Points, user.LoyaltyPoints) nghĩa là khách
            // tích điểm -> đổi hết lấy voucher ngay -> trả hàng sẽ không bị trừ gì cả, giữ nguyên
            // voucher đã đổi dù đơn hàng gốc không còn hợp lệ — rò rỉ giá trị miễn phí.
            user.LoyaltyPoints -= earnTx.Points;
            _context.LoyaltyPointTransactions.Add(new LoyaltyPointTransaction
            {
                UserId = earnTx.UserId,
                Points = -earnTx.Points,
                Reason = $"Hoàn điểm do đơn hàng #{orderId} bị trả hàng",
                OrderId = orderId
            });

            await _context.SaveChangesAsync();
        }
    }
}
