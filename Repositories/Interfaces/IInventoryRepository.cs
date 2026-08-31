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
        // Khoá 1 lô cụ thể (UPDLOCK, ROWLOCK) trước khi đọc-sửa QuantityRemaining — dùng ở Dispose/Adjust
        // để đồng bộ mức độ chặt chẽ với DeductStockFEFO/RestoreStockFEFO, tránh mất cập nhật khi 2 nhân
        // viên cùng thao tác 1 lô đồng thời.
        Task<StockBatch> GetBatchByIdForUpdate(int id);
        Task<List<StockBatch>> GetBatchesExpiringWithin(int daysAhead);
        Task<StockBatch> AddBatch(StockBatch batch);
        Task<int> GetTotalRemainingForMedicine(int medicineId);
        Task RecomputeStockCaches(int medicineId, int warehouseId);
        // Đồng bộ Medicine.Price theo SellPrice của batchId — CHỈ áp dụng nếu batch đó đang là lô FEFO
        // đầu hàng đợi (hết hạn sớm nhất, còn hàng) NGAY LÚC gọi. Dùng ngay sau khi nhập/cập nhật 1 lô có
        // đặt giá bán riêng — không hồi tố: nếu 1 lô cũ tự lên làm lô đầu về sau (do lô khác bán/hủy hết),
        // giá lưu sẵn trong lô đó sẽ không tự ghi đè Price hiện tại nữa.
        Task SyncPriceFromNewBatchIfFrontAsync(int medicineId, int batchId);
        Task<Medicine> GetMedicineById(int id);
        Task SaveChangesAsync();

        // Báo cáo lãi gộp: các giao dịch xuất kho có gắn lô + trạng thái đơn hàng liên quan,
        // để chỉ tính là "đã bán" khi đơn đã thanh toán và không bị hủy/trả hàng.
        Task<List<InventoryTransaction>> GetExportTransactionsWithBatch();
        // Như trên nhưng lọc theo khoảng thời gian giao dịch — dùng cho báo cáo lãi gộp tổng hợp theo kỳ
        // (gộp mọi sản phẩm), khác GetExportTransactionsWithBatch (không lọc) dùng cho báo cáo theo 1 lô/sản phẩm.
        Task<List<InventoryTransaction>> GetExportTransactionsWithBatchInRange(DateTime from, DateTime to);
        // Các giao dịch xuất kho gốc (Type=Export) khớp đúng 1 ReferenceId cụ thể — dùng khi hoàn kho
        // (RestoreStockFEFO) để hoàn đúng vào (các) lô đã thực sự xuất, thay vì luôn dồn vào lô hết hạn
        // sớm nhất hiện tại.
        Task<List<InventoryTransaction>> GetExportTransactionsForReference(int medicineId, int warehouseId, string referenceId);
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
