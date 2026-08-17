using BusinessObjects;
using Microsoft.EntityFrameworkCore.Storage;

namespace Repositories.Interfaces
{
    public interface IInventoryRepository
    {
        // Trả về null nếu context đã có transaction đang chạy (caller đã tự mở transaction bao ngoài,
        // ví dụ PrescriptionService.Create / OrdersController.CreateOrder) — khi đó không mở transaction
        // lồng nhau (EF Core không hỗ trợ), caller giữ vai trò commit/rollback.
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<InventoryStock> GetStock(int medicineId, int warehouseId);
        Task<List<InventoryStock>> GetStockByWarehouse(int warehouseId);
        Task<List<InventoryStock>> GetStockByMedicine(int medicineId);
        Task<List<InventoryStock>> GetAllStock();
        Task UpsertStock(InventoryStock stock);
        Task<InventoryTransaction> AddTransaction(InventoryTransaction transaction);
        Task<List<InventoryTransaction>> GetTransactions(int? medicineId, int? warehouseId);

        // Quản lý theo lô (batch/lot)
        Task<StockBatch> GetBatchById(int id);
        Task<StockBatch> GetBatchByNumber(int medicineId, int warehouseId, string batchNumber);
        Task<List<StockBatch>> GetBatchesByMedicine(int medicineId, int? warehouseId);
        Task<List<StockBatch>> GetBatchesByWarehouse(int warehouseId);
        Task<List<StockBatch>> GetActiveBatchesForFEFO(int medicineId, int warehouseId);
        // Như trên nhưng khoá dòng (UPDLOCK, ROWLOCK) — dùng trong giao dịch trừ/hoàn kho để tránh bán vượt tồn kho khi có nhiều request đồng thời.
        Task<List<StockBatch>> GetActiveBatchesForFEFOForUpdate(int medicineId, int warehouseId);
        Task<List<StockBatch>> GetBatchesExpiringWithin(int daysAhead);
        Task<StockBatch> AddBatch(StockBatch batch);
        Task<int> GetTotalRemainingForMedicine(int medicineId);
        Task RecomputeStockCaches(int medicineId, int warehouseId);
        Task<Medicine> GetMedicineById(int id);
        Task SaveChangesAsync();

        // Báo cáo lãi gộp: các giao dịch xuất kho có gắn lô + trạng thái đơn hàng liên quan,
        // để chỉ tính là "đã bán" khi đơn đã thanh toán và không bị hủy/trả hàng.
        Task<List<InventoryTransaction>> GetExportTransactionsWithBatch();
        Task<Dictionary<int, (string? Status, string? PaymentStatus)>> GetOrderStatusMap();
        Task<List<StockBatch>> GetBatchesWithCost(int? warehouseId, int? medicineId);
        // Giá bán thực tế tại thời điểm bán, để tính doanh thu theo lô chính xác thay vì dùng giá hiện tại.
        Task<Dictionary<(int OrderId, int MedicineId), decimal>> GetOrderItemPriceMap();
        Task<Dictionary<(int PrescriptionId, int MedicineId), decimal?>> GetPrescriptionItemPriceMap();

        // Quản lý Flash Sale
        Task<FlashSale> AddFlashSale(FlashSale flashSale);
        Task<FlashSale> GetActiveFlashSaleByMedicine(int medicineId);
        Task<List<FlashSale>> GetFlashSales(bool activeOnly);
    }
}
