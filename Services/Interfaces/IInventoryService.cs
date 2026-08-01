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
    }
}
