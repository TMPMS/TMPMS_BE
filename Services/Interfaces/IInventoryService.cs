using TMPMS.DTOs;

namespace Services.Interfaces
{
    public interface IInventoryService
    {
        Task<InventoryTransactionResponseDTO> CreateTransaction(StockTransactionCreateDTO dto);
        Task<List<InventoryStockResponseDTO>> GetStockByWarehouse(int warehouseId);
        Task<List<InventoryStockResponseDTO>> GetStockByMedicine(int medicineId);
        Task<List<InventoryStockResponseDTO>> GetAllStock();
        Task<List<InventoryTransactionResponseDTO>> GetTransactions(int? medicineId, int? warehouseId);
        Task<List<LowStockAlertDTO>> GetLowStockAlerts(int threshold);
        Task<List<ExpiryAlertDTO>> GetExpiryAlerts(int daysAhead);

        // Quản lý theo lô (batch/lot)
        Task<StockBatchResponseDTO> CreateBatch(StockBatchCreateDTO dto);
        Task<List<StockBatchResponseDTO>> GetBatchesByMedicine(int medicineId, int? warehouseId);
        Task<List<StockBatchResponseDTO>> GetBatchesByWarehouse(int warehouseId);
        Task<StockBatchResponseDTO> DisposeBatch(int batchId, BatchDisposeDTO dto);
        Task<StockBatchResponseDTO> AdjustBatch(int batchId, BatchAdjustDTO dto);

        // Xuất kho FEFO (hết hạn sớm nhất xuất trước) — dùng khi bán/kê đơn
        Task DeductStockFEFO(int medicineId, int warehouseId, int quantity, string referenceId);
        // Hoàn kho khi hủy đơn — cộng lại vào lô còn hạn gần nhất
        // originalExportReferenceId: ReferenceId THẬT của giao dịch xuất kho gốc (vd "ORDER-5", "RX-12")
        // — truyền tường minh để hoàn đúng vào lô đã xuất, thay vì dựa vào quy ước đặt tên (hậu tố
        // "-RESTOCK") vốn dễ vỡ nếu có code khác không tuân theo convention.
        Task RestoreStockFEFO(int medicineId, int warehouseId, int quantity, string referenceId, string? originalExportReferenceId = null);

        // Flash Sale cho hàng gần hết hạn
        Task<List<FlashSaleCandidateDTO>> GetFlashSaleCandidates(int daysThreshold);
        Task<FlashSaleCandidateDTO> ApplyFlashSale(int medicineId, ApplyFlashSaleDTO dto, int? staffId);
        Task RemoveFlashSale(int medicineId, int? staffId);
        Task<List<FlashSaleRecordDTO>> GetFlashSales(bool activeOnly);
        // Danh sách Flash Sale công khai (đang chạy + sắp diễn ra) cho trang khách hàng
        Task<List<PublicFlashSaleDTO>> GetActiveFlashSalesForCustomer();
        // Quét định kỳ: kích hoạt Flash Sale đã tới giờ bắt đầu, tự gỡ Flash Sale đã hết giờ/hết suất
        Task SweepFlashSales();

        // Báo cáo lãi gộp ước tính theo lô
        Task<List<BatchProfitDTO>> GetBatchProfitReport(int? warehouseId, int? medicineId);
        // Báo cáo lãi gộp ước tính tổng hợp theo kỳ (ngày/tháng/năm), gộp mọi sản phẩm — cho phép xem
        // lãi gộp toàn cửa hàng trong 1 màn hình thay vì phải chọn từng sản phẩm như GetBatchProfitReport.
        Task<List<ProfitPointDTO>> GetProfitByPeriod(System.DateTime from, System.DateTime to, string groupBy);
        // Gợi ý nhập hàng dựa trên tốc độ bán trung bình lookbackDays ngày gần nhất, chiếu theo
        // leadTimeDays ngày chờ hàng về, trừ tồn kho hiện có.
        Task<List<ReorderSuggestionDTO>> GetReorderSuggestions(int lookbackDays, int leadTimeDays);
    }
}
