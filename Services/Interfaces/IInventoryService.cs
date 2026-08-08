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
        Task RestoreStockFEFO(int medicineId, int warehouseId, int quantity, string referenceId);

        // Flash Sale cho hàng gần hết hạn
        Task<List<FlashSaleCandidateDTO>> GetFlashSaleCandidates(int daysThreshold);
        Task<FlashSaleCandidateDTO> ApplyFlashSale(int medicineId, ApplyFlashSaleDTO dto, int? staffId);
        Task RemoveFlashSale(int medicineId, int? staffId);
        Task<List<FlashSaleRecordDTO>> GetFlashSales(bool activeOnly);

        // Báo cáo lãi gộp ước tính theo lô
        Task<List<BatchProfitDTO>> GetBatchProfitReport(int? warehouseId, int? medicineId);
    }
}
