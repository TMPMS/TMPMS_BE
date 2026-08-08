using System;

namespace BusinessObjects
{
    public class Voucher
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string DiscountType { get; set; } = "percent"; // "percent" or "flat"
        public decimal DiscountValue { get; set; }
        public decimal MinOrderValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
        public int UsageLimit { get; set; } = 100;
        public int UsedCount { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // "product" (giảm giá sản phẩm) hoặc "shipping" (giảm phí vận chuyển)
        public string Type { get; set; } = "product";

        // null = voucher công khai (ai cũng dùng được); có giá trị = voucher cá nhân
        // (VD trúng từ vòng quay may mắn), chỉ chủ sở hữu mới được áp dụng.
        public int? OwnerUserId { get; set; }

        // true = đây là mẫu phần thưởng của vòng quay may mắn, không phải mã công khai
        // để khách nhập tay — khi trúng thưởng, server sẽ nhân bản mẫu này thành 1 voucher
        // cá nhân riêng cho người thắng.
        public bool IsWheelPrize { get; set; } = false;

        // Trọng số quay ngẫu nhiên có trọng số, chỉ có ý nghĩa khi IsWheelPrize = true.
        public int Weight { get; set; } = 0;

        // Concurrency token — chống race condition khi 2 đơn hàng cùng tăng UsedCount đồng thời.
        public byte[]? RowVersion { get; set; }
    }
}
