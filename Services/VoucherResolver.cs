using System;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using TMPMS.Data;

namespace TMPMS.Services
{
    public class VoucherResolveResult
    {
        public Voucher? Voucher { get; set; }
        public string? Error { get; set; }
    }

    // Logic tra cứu + kiểm tra hợp lệ voucher dùng chung giữa VouchersController (preview /validate)
    // và OrdersController (áp dụng thật khi checkout) — tránh 2 nơi lệch nhau.
    public static class VoucherResolver
    {
        public static async Task<VoucherResolveResult> ResolveAsync(
            TMPMSDbContext context, string? code, string expectedType, int? currentUserId)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return new VoucherResolveResult();
            }

            var normalized = code.Trim().ToUpperInvariant();
            var voucher = await context.Vouchers.FirstOrDefaultAsync(v => v.Code == normalized);

            const string genericError = "Mã voucher không hợp lệ hoặc đã hết hạn.";

            if (voucher == null || !voucher.IsActive || voucher.IsWheelPrize ||
                (voucher.EndDate != null && voucher.EndDate <= DateTime.UtcNow) ||
                voucher.UsedCount >= voucher.UsageLimit)
            {
                return new VoucherResolveResult { Error = genericError };
            }

            // Voucher cá nhân (trúng từ vòng quay) — chỉ chủ sở hữu mới dùng được.
            // Không tiết lộ lý do cụ thể để tránh dò mã của người khác.
            if (voucher.OwnerUserId != null && voucher.OwnerUserId != currentUserId)
            {
                return new VoucherResolveResult { Error = genericError };
            }

            if (voucher.Type != expectedType)
            {
                var expectedLabel = expectedType == "shipping" ? "phí vận chuyển" : "sản phẩm";
                var actualLabel = voucher.Type == "shipping" ? "phí vận chuyển" : "sản phẩm";
                return new VoucherResolveResult
                {
                    Error = $"Mã \"{voucher.Code}\" là voucher giảm {actualLabel}, vui lòng nhập vào ô giảm {expectedLabel}."
                };
            }

            return new VoucherResolveResult { Voucher = voucher };
        }

        // Tính số tiền giảm cho voucher trên 1 khoản tiền gốc (subtotal cho voucher sản phẩm,
        // shippingFee cho voucher ship) — không bao giờ giảm quá khoản tiền gốc đó.
        public static decimal ComputeDiscount(Voucher voucher, decimal baseAmount)
        {
            decimal discount = voucher.DiscountType == "percent"
                ? baseAmount * voucher.DiscountValue / 100
                : voucher.DiscountValue;

            if (voucher.MaxDiscount.HasValue)
            {
                discount = Math.Min(discount, voucher.MaxDiscount.Value);
            }

            return Math.Max(0m, Math.Min(discount, baseAmount));
        }
    }
}
