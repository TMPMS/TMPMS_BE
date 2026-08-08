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
        // Không truyền = dùng mức giảm đề xuất theo số ngày còn hạn
        public int? DiscountPercent { get; set; }
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
    }

    // Báo cáo lãi gộp ước tính theo lô — Doanh thu dùng GIÁ BÁN HIỆN TẠI của sản phẩm
    // (không truy hồi giá bán thực tế tại thời điểm bán từng đơn), nên đây là số ƯỚC TÍNH, không phải sổ sách kế toán chính xác.
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
    }
}
