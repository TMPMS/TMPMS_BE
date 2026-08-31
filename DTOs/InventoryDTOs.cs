using System;

namespace TMPMS.DTOs
{
    // Nhập / Xuất kho
    public class StockTransactionCreateDTO
    {
        public int MedicineId { get; set; }
        public int WarehouseId { get; set; }
        public string Type { get; set; }   // Import, Export, Adjustment
        public int Quantity { get; set; }
        public string ReferenceId { get; set; } // Mã đơn hàng/PO liên quan (nếu có)
    }

    public class InventoryStockResponseDTO
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public int Quantity { get; set; }
    }

    public class InventoryTransactionResponseDTO
    {
        public int Id { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public string Type { get; set; }
        public int Quantity { get; set; }
        public string ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LowStockAlertDTO
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public int CurrentQuantity { get; set; }
        public int Threshold { get; set; }
    }

    public class ExpiryAlertDTO
    {
        public int BatchId { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public string BatchNumber { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int DaysRemaining { get; set; }
        public int QuantityRemaining { get; set; }
        // Critical (<=7 ngày) / Warning (<=30 ngày) / Notice (<=90 ngày)
        public string Severity { get; set; }
    }

    // Nhập lô hàng mới vào kho
    public class StockBatchCreateDTO
    {
        public int MedicineId { get; set; }
        public int WarehouseId { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int Quantity { get; set; }
        public decimal? UnitCostPrice { get; set; }
        // Giá bán riêng cho lô này — khi lô này là lô FEFO đang bán, Medicine.Price tự đồng bộ theo giá
        // này (trừ khi đang có Flash Sale). Không truyền = không đặt giá riêng cho lô.
        public decimal? SellPrice { get; set; }
        public int? SupplierId { get; set; }
        public string? RegistrationNumber { get; set; } // Số đăng ký (SĐK)
        public string? StorageCondition { get; set; } // Kho Thường / Kho Mát / Cold Chain (Vắc-xin)
        public string? QcStatus { get; set; } = "Pass"; // Pass (Đạt QC -> Active) | Fail (Không đạt -> Biệt trữ Quarantine)
        public string? Note { get; set; }
    }

    public class StockBatchResponseDTO
    {
        public int Id { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public string BatchNumber { get; set; }
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int QuantityReceived { get; set; }
        public int QuantityRemaining { get; set; }
        public decimal? UnitCostPrice { get; set; }
        public decimal? SellPrice { get; set; }
        public int? SupplierId { get; set; }
        public string SupplierName { get; set; }
        public DateTime ReceivedAt { get; set; }
        public string Status { get; set; }
        public int DaysUntilExpiry { get; set; }
        public string Note { get; set; }
    }

    public class BatchDisposeDTO
    {
        // Không truyền = hủy toàn bộ số lượng còn lại của lô
        public int? Quantity { get; set; }
        public string? Reason { get; set; }
    }

    public class BatchAdjustDTO
    {
        public int QuantityRemaining { get; set; }
        public string? Reason { get; set; }
    }

    public class FlashSaleCandidateDTO
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string ImageUrl { get; set; }
        public decimal? Price { get; set; }
        public decimal? OldPrice { get; set; }
        public int? Discount { get; set; }
        public string Unit { get; set; }
        public string Origin { get; set; }
        public int BatchId { get; set; }
        public string BatchNumber { get; set; }
        public DateTime NearestExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
        public int QuantityRemaining { get; set; }
        public int SuggestedDiscountPercent { get; set; }
        public bool IsOnFlashSale { get; set; }
    }

    public class ApplyFlashSaleDTO
    {
        // Không truyền = dùng mức giảm đề xuất theo số ngày còn hạn (chỉ khả dụng nếu có lô sắp hết hạn)
        public int? DiscountPercent { get; set; }

        // Không truyền/null = áp dụng ngay. Truyền thời điểm tương lai để hẹn giờ bắt đầu.
        public DateTime? StartTime { get; set; }

        // Không truyền/null = không tự kết thúc, chỉ gỡ thủ công.
        public DateTime? EndTime { get; set; }

        // Không truyền/null = không giới hạn số lượng bán theo giá sale.
        public int? QuantityLimit { get; set; }
    }

    // Bản ghi trong bảng quản lý Flash Sale (Admin) — khác FlashSaleCandidateDTO ở chỗ đây là
    // dữ liệu đã lưu (ai áp dụng, khi nào, đang bật hay đã gỡ), không phải danh sách gợi ý.
    public class FlashSaleRecordDTO
    {
        public int Id { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string ImageUrl { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int DiscountPercent { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? BatchExpiryDate { get; set; }
        public int? DaysUntilExpiryAtApply { get; set; }
        public DateTime AppliedAt { get; set; }
        public string? AppliedByStaffName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? RemovedAt { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? QuantityLimit { get; set; }
        public int QuantitySold { get; set; }
        // Scheduled (chưa tới giờ bắt đầu) / Running (đang diễn ra) / Ended (đã gỡ/hết hạn/hết suất)
        public string Status { get; set; }
    }

    // Danh sách Flash Sale công khai cho trang khách hàng — gọn hơn FlashSaleRecordDTO (không lộ
    // thông tin nội bộ như ai áp dụng, lô hàng).
    public class PublicFlashSaleDTO
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string ImageUrl { get; set; }
        public string Unit { get; set; }
        public string Origin { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int DiscountPercent { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? QuantityLimit { get; set; }
        public int QuantitySold { get; set; }
        public int StockQuantity { get; set; }
        // Scheduled / Running
        public string Status { get; set; }
    }

    // Báo cáo lãi gộp theo lô. Doanh thu dùng GIÁ BÁN THỰC TẾ tại thời điểm bán (OrderItem.Price cho đơn
    // hàng, PrescriptionItem.UnitPrice cho đơn thuốc). Chỉ khi không tra được giá thực (dữ liệu tạo trước
    // khi có snapshot giá đơn thuốc) mới rơi về giá hiện tại của Medicine — khi đó IsEstimated = true để
    // FE có thể hiển thị ghi chú "số liệu ước tính" cho lô đó.
    public class BatchProfitDTO
    {
        public int BatchId { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string BatchNumber { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public int QuantitySold { get; set; }
        public decimal UnitCostPrice { get; set; }
        public decimal? CurrentSellPrice { get; set; }
        public decimal EstimatedRevenue { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal EstimatedGrossProfit { get; set; }
        public decimal? GrossMarginPercent { get; set; }
        public bool IsEstimated { get; set; }
    }

    // Lãi gộp ước tính tổng hợp theo kỳ (ngày/tháng/năm), gộp mọi sản phẩm — bổ sung cho BatchProfitDTO
    // (vốn chỉ xem được từng sản phẩm một). Cùng quy tắc "đã bán" và cách lấy giá bán thực tế như
    // BatchProfitDTO (xem GetProfitByPeriod).
    public class ProfitPointDTO
    {
        public string Period { get; set; }
        public decimal EstimatedRevenue { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal EstimatedGrossProfit { get; set; }
        public decimal? GrossMarginPercent { get; set; }
        public bool IsEstimated { get; set; }
    }

    // Gợi ý số lượng cần nhập thêm — ước tính từ tốc độ bán trung bình gần đây (lookbackDays) chiếu
    // theo thời gian chờ hàng về (leadTimeDays), trừ đi tồn kho hiện có. Chỉ là gợi ý tham khảo, không
    // tính đến mùa vụ/khuyến mãi sắp tới.
    public class ReorderSuggestionDTO
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int CurrentStock { get; set; }
        public decimal AvgDailySales { get; set; }
        public int LeadTimeDays { get; set; }
        public int SuggestedReorderQuantity { get; set; }
    }
}
