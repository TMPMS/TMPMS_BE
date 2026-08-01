using BusinessObjects;

namespace Repositories.Interfaces
{
    public interface IInventoryRepository
    {
        Task<InventoryStock> GetStock(int medicineId, int warehouseId);
        Task<List<InventoryStock>> GetStockByWarehouse(int warehouseId);
        Task<List<InventoryStock>> GetStockByMedicine(int medicineId);
        Task<List<InventoryStock>> GetAllStock();
        Task UpsertStock(InventoryStock stock);
        Task<InventoryTransaction> AddTransaction(InventoryTransaction transaction);
        Task<List<InventoryTransaction>> GetTransactions(int? medicineId, int? warehouseId);
        Task<List<Medicine>> GetLowStockMedicines(int threshold);
        Task<List<Medicine>> GetExpiringMedicines(int daysAhead);
    }
}
