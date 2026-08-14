using System;

namespace BusinessObjects
{
    // Bản ghi quản lý Flash Sale — lịch sử/trạng thái áp dụng giảm giá cho thuốc gần hết hạn,
    // tách biệt khỏi Medicine.Discount (vẫn dùng để hiển thị giá giảm ngoài cửa hàng) để Admin
    // có một bảng riêng theo dõi: ai áp dụng, khi nào, giảm bao nhiêu %, đang bật hay đã gỡ.
    public class FlashSale
    {
        public int Id { get; set; }

        public int MedicineId { get; set; }

        public int? BatchId { get; set; }

        public decimal OriginalPrice { get; set; }

        public decimal SalePrice { get; set; }

        public int DiscountPercent { get; set; }

        // Hẹn giờ chạy Flash Sale — null = áp dụng ngay khi tạo. Nếu ở tương lai, giá chỉ được
        // đổi thực sự khi FlashSaleBackgroundService quét tới đúng giờ (không đổi giá ngay lúc tạo).
        public DateTime? StartTime { get; set; }

        // null = không giới hạn thời gian, chỉ kết thúc khi Admin gỡ thủ công.
        public DateTime? EndTime { get; set; }

        // null = không giới hạn số lượng bán theo giá sale.
        public int? QuantityLimit { get; set; }

        public int QuantitySold { get; set; } = 0;

        public DateTime? BatchExpiryDate { get; set; }

        public int? DaysUntilExpiryAtApply { get; set; }

        public DateTime AppliedAt { get; set; }

        public int? AppliedByStaffId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? RemovedAt { get; set; }

        public int? RemovedByStaffId { get; set; }

        public Medicine? Medicine { get; set; }

        public StockBatch? Batch { get; set; }

        public User? AppliedByStaff { get; set; }

        public User? RemovedByStaff { get; set; }
    }
}
