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
