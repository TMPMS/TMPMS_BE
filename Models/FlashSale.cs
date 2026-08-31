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

        // true = đã ghi SalePrice vào Medicine.Price ít nhất 1 lần (ngay lúc tạo nếu áp dụng ngay, hoặc
        // lúc FlashSaleBackgroundService quét tới đúng StartTime nếu hẹn giờ). Dùng để SweepFlashSales chỉ
        // ép giá đúng 1 LẦN DUY NHẤT khi Flash Sale thực sự bắt đầu — sau đó nếu Admin/Dược sĩ chủ động sửa
        // giá tay trong lúc sale đang chạy, giá đó phải được giữ nguyên, không bị job quét mỗi phút âm thầm
        // ép trả lại giá sale (đây từng là bug khiến sửa giá bán không "ăn" khi sản phẩm đang có Flash Sale).
        public bool PriceApplied { get; set; }

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
