using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Repositories.Interfaces;
using TMPMS.Data;

namespace TMPMS.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly TMPMSDbContext _context;
        public InventoryRepository(TMPMSDbContext context) => _context = context;

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            if (_context.Database.CurrentTransaction != null) return null;
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task<InventoryStock> GetStock(int medicineId, int warehouseId)
        {
            var stock = await _context.InventoryStocks
                .Include(s => s.Medicine)
                .Include(s => s.Warehouse)
                .FirstOrDefaultAsync(s => s.MedicineId == medicineId && s.WarehouseId == warehouseId);

            if (stock == null)
            {
                var med = await _context.Medicines.FindAsync(medicineId);
                if (med != null)
                {
                    return new InventoryStock
                    {
                        MedicineId = medicineId,
                        WarehouseId = warehouseId,
                        Quantity = med.StockQuantity
                    };
                }
            }

            return stock;
        }

        public async Task<List<InventoryStock>> GetStockByWarehouse(int warehouseId)
        {
            return await _context.InventoryStocks
                .Include(s => s.Medicine)
                .Include(s => s.Warehouse)
                .Where(s => s.WarehouseId == warehouseId)
                .ToListAsync();
        }

        public async Task<List<InventoryStock>> GetStockByMedicine(int medicineId)
        {
            return await _context.InventoryStocks
                .Include(s => s.Medicine)
                .Include(s => s.Warehouse)
                .Where(s => s.MedicineId == medicineId)
                .ToListAsync();
        }

        public async Task<List<InventoryStock>> GetAllStock()
        {
            return await _context.InventoryStocks
                .Include(s => s.Medicine)
                .Include(s => s.Warehouse)
                .ToListAsync();
        }

        public async Task UpsertStock(InventoryStock stock)
        {
            var existing = await _context.InventoryStocks
                .FirstOrDefaultAsync(s => s.MedicineId == stock.MedicineId && s.WarehouseId == stock.WarehouseId);

            if (existing == null)
            {
                _context.InventoryStocks.Add(stock);
            }
            else
            {
                existing.Quantity = stock.Quantity;
            }

            var med = await _context.Medicines.FindAsync(stock.MedicineId);
            if (med != null)
            {
                med.StockQuantity = stock.Quantity;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<InventoryTransaction> AddTransaction(InventoryTransaction transaction)
        {
            _context.InventoryTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<List<InventoryTransaction>> GetTransactions(int? medicineId, int? warehouseId)
        {
            var query = _context.InventoryTransactions
                .Include(t => t.Medicine)
                .Include(t => t.Warehouse)
                .AsQueryable();

            if (medicineId.HasValue) query = query.Where(t => t.MedicineId == medicineId.Value);
            if (warehouseId.HasValue) query = query.Where(t => t.WarehouseId == warehouseId.Value);

            return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        }

        public async Task<StockBatch> GetBatchById(int id)
        {
            return await _context.StockBatches
                .Include(b => b.Medicine)
                .Include(b => b.Warehouse)
                .Include(b => b.Supplier)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<StockBatch> GetBatchByNumber(int medicineId, int warehouseId, string batchNumber)
        {
            return await _context.StockBatches.FirstOrDefaultAsync(b =>
                b.MedicineId == medicineId && b.WarehouseId == warehouseId && b.BatchNumber == batchNumber);
        }

        public async Task<List<StockBatch>> GetBatchesByMedicine(int medicineId, int? warehouseId)
        {
            var query = _context.StockBatches
                .Include(b => b.Warehouse)
                .Include(b => b.Supplier)
                .Where(b => b.MedicineId == medicineId);

            if (warehouseId.HasValue) query = query.Where(b => b.WarehouseId == warehouseId.Value);

            return await query.OrderBy(b => b.ExpiryDate).ToListAsync();
        }

        public async Task<List<StockBatch>> GetBatchesByWarehouse(int warehouseId)
        {
            return await _context.StockBatches
                .Include(b => b.Medicine)
                .Include(b => b.Supplier)
                .Where(b => b.WarehouseId == warehouseId)
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync();
        }

        // FEFO: lô hết hạn sớm nhất được xuất trước
        // Dùng .Date (bỏ giờ) để khớp với cách InventoryService tính hạn dùng hiển thị cho người dùng
        // (Severity/ComputeDisplayStatus) — tránh trường hợp lô hết hạn "hôm nay" bị loại khỏi FEFO
        // ngay từ 00:00:01 trong khi UI vẫn báo "còn hàng, hết hạn hôm nay".
        public async Task<List<StockBatch>> GetActiveBatchesForFEFO(int medicineId, int warehouseId)
        {
            var today = DateTime.Now.Date;
            return await _context.StockBatches
                .Where(b => b.MedicineId == medicineId
                    && b.WarehouseId == warehouseId
                    && b.Status == StockBatchStatus.Active
                    && b.QuantityRemaining > 0
                    && b.ExpiryDate.Date >= today)
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync();
        }

        // Như GetActiveBatchesForFEFO nhưng khoá dòng bằng UPDLOCK/ROWLOCK trong transaction hiện tại,
        // để hai request trừ kho đồng thời không cùng đọc số dư cũ rồi cùng ghi đè (tránh bán vượt tồn kho).
        public async Task<List<StockBatch>> GetActiveBatchesForFEFOForUpdate(int medicineId, int warehouseId)
        {
            var today = DateTime.Now.Date;
            return await _context.StockBatches
                .FromSqlInterpolated($@"SELECT * FROM StockBatches WITH (UPDLOCK, ROWLOCK)
                    WHERE MedicineId = {medicineId} AND WarehouseId = {warehouseId}
                    AND Status = {StockBatchStatus.Active} AND QuantityRemaining > 0 AND CAST(ExpiryDate AS DATE) >= {today}")
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync();
        }

        // Như GetActiveBatchesForFEFOForUpdate nhưng khoá đúng 1 lô theo Id — dùng cho Dispose/Adjust
        // để 2 nhân viên không cùng đọc-rồi-ghi-đè QuantityRemaining của cùng 1 lô.
        public async Task<StockBatch> GetBatchByIdForUpdate(int id)
        {
            return (await _context.StockBatches
                .FromSqlInterpolated($"SELECT * FROM StockBatches WITH (UPDLOCK, ROWLOCK) WHERE Id = {id}")
                .ToListAsync())
                .FirstOrDefault();
        }

        public async Task<List<StockBatch>> GetBatchesExpiringWithin(int daysAhead)
        {
            var limitDate = DateTime.Now.AddDays(daysAhead);
            return await _context.StockBatches
                .Include(b => b.Medicine)
                .Include(b => b.Warehouse)
                .Where(b => b.Status != StockBatchStatus.Disposed
                    && b.Status != StockBatchStatus.Depleted
                    && b.QuantityRemaining > 0
                    && b.ExpiryDate <= limitDate)
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync();
        }

        public async Task<StockBatch> AddBatch(StockBatch batch)
        {
            _context.StockBatches.Add(batch);
            await _context.SaveChangesAsync();
            return batch;
        }

        public async Task<int> GetTotalRemainingForMedicine(int medicineId)
        {
            var today = DateTime.Now.Date;
            return await _context.StockBatches
                .Where(b => b.MedicineId == medicineId && b.Status == StockBatchStatus.Active && b.ExpiryDate.Date >= today)
                .SumAsync(b => (int?)b.QuantityRemaining) ?? 0;
        }

        // Đồng bộ lại số liệu tồn kho cache (InventoryStock theo kho + StockQuantity tổng trên Medicine)
        // từ tổng QuantityRemaining của các lô còn hạn — nguồn sự thật luôn là StockBatches.
        public async Task RecomputeStockCaches(int medicineId, int warehouseId)
        {
            var today = DateTime.Now.Date;
            var warehouseTotal = await _context.StockBatches
                .Where(b => b.MedicineId == medicineId && b.WarehouseId == warehouseId
                    && b.Status == StockBatchStatus.Active && b.ExpiryDate.Date >= today)
                .SumAsync(b => (int?)b.QuantityRemaining) ?? 0;

            var stock = await _context.InventoryStocks
                .FirstOrDefaultAsync(s => s.MedicineId == medicineId && s.WarehouseId == warehouseId);
            if (stock == null)
            {
                _context.InventoryStocks.Add(new InventoryStock { MedicineId = medicineId, WarehouseId = warehouseId, Quantity = warehouseTotal });
            }
            else
            {
                stock.Quantity = warehouseTotal;
            }

            var medicineTotal = await GetTotalRemainingForMedicine(medicineId);
            var med = await _context.Medicines.FindAsync(medicineId);
            if (med != null)
            {
                med.StockQuantity = medicineTotal;
                await SyncPriceFromActiveBatchAsync(med);
            }

            await _context.SaveChangesAsync();
        }

        // Đồng bộ giá bán theo lô FEFO đang bán (hết hạn sớm nhất, còn hàng) — nếu lô đó có đặt SellPrice
        // riêng. Bỏ qua khi đang có Flash Sale Active cho sản phẩm này: Flash Sale tự quản lý Price/OldPrice
        // riêng (xem InventoryService.ApplyFlashSale/RemoveFlashSale/SweepFlashSales) và luôn được ưu tiên
        // cao hơn giá theo lô — nếu không, việc đồng bộ này (chạy sau MỌI thao tác ảnh hưởng lô, kể cả
        // không liên quan tới đợt Flash Sale) có thể vô tình ghi đè mất giá đang giảm.
        private async Task SyncPriceFromActiveBatchAsync(Medicine med)
        {
            var hasActiveFlashSale = await _context.FlashSales.AnyAsync(f => f.MedicineId == med.Id && f.IsActive);
            if (hasActiveFlashSale) return;

            var today = DateTime.Now.Date;
            var frontBatch = await _context.StockBatches
                .Where(b => b.MedicineId == med.Id && b.Status == StockBatchStatus.Active
                    && b.QuantityRemaining > 0 && b.ExpiryDate.Date >= today)
                .OrderBy(b => b.ExpiryDate)
                .FirstOrDefaultAsync();

            // Chỉ đồng bộ khi LÔ ĐẦU HÀNG ĐỢI THỰC SỰ ĐỔI so với lần đồng bộ trước (PricedFromBatchId) —
            // nếu vẫn là lô cũ (chưa có gì thay đổi thật sự), bỏ qua để Admin/Dược sĩ tự do sửa giá tay
            // ở giữa 2 lần chuyển lô mà không bị hàm này chạy lại (do các sự kiện kho khác không liên
            // quan, vd một lô khác vừa bị hủy) ghi đè mất.
            if (frontBatch?.Id == med.PricedFromBatchId) return;

            if (frontBatch?.SellPrice != null)
            {
                med.Price = frontBatch.SellPrice;
            }
            med.PricedFromBatchId = frontBatch?.Id;
        }

        public async Task<Medicine> GetMedicineById(int id) => await _context.Medicines.FindAsync(id);

        public async Task<List<InventoryTransaction>> GetExportTransactionsWithBatch()
        {
            return await _context.InventoryTransactions
                .Where(t => t.Type == "Export" && t.StockBatchId != null)
                .ToListAsync();
        }

        public async Task<List<InventoryTransaction>> GetExportTransactionsWithBatchInRange(DateTime from, DateTime to)
        {
            return await _context.InventoryTransactions
                .Include(t => t.StockBatch)
                .Include(t => t.Medicine)
                .Where(t => t.Type == "Export" && t.StockBatchId != null && t.CreatedAt >= from && t.CreatedAt <= to)
                .ToListAsync();
        }

        public async Task<List<InventoryTransaction>> GetExportTransactionsForReference(int medicineId, int warehouseId, string referenceId)
        {
            return await _context.InventoryTransactions
                .Where(t => t.Type == "Export" && t.MedicineId == medicineId && t.WarehouseId == warehouseId
                    && t.ReferenceId == referenceId && t.StockBatchId != null)
                .ToListAsync();
        }

        public async Task<Dictionary<int, (string? Status, string? PaymentStatus)>> GetOrderStatusMap()
        {
            var orders = await _context.Orders
                .Select(o => new { o.Id, o.Status, o.PaymentStatus })
                .ToListAsync();
            return orders.ToDictionary(o => o.Id, o => (o.Status, o.PaymentStatus));
        }

        public async Task<List<StockBatch>> GetBatchesWithCost(int? warehouseId, int? medicineId)
        {
            var query = _context.StockBatches
                .Include(b => b.Medicine)
                .Include(b => b.Warehouse)
                .Where(b => b.UnitCostPrice != null)
                .AsQueryable();

            if (warehouseId.HasValue) query = query.Where(b => b.WarehouseId == warehouseId.Value);
            if (medicineId.HasValue) query = query.Where(b => b.MedicineId == medicineId.Value);

            return await query.OrderByDescending(b => b.ReceivedAt).ToListAsync();
        }

        public async Task<Dictionary<(int OrderId, int MedicineId), decimal>> GetOrderItemPriceMap()
        {
            var items = await _context.OrderItems
                .Select(oi => new { oi.OrderId, oi.MedicineId, oi.Price })
                .ToListAsync();
            // Nhóm để tránh lỗi trùng khóa nếu 1 đơn có 2 dòng cùng MedicineId — lấy dòng đầu tiên.
            return items
                .GroupBy(x => (x.OrderId, x.MedicineId))
                .ToDictionary(g => g.Key, g => g.First().Price);
        }

        public async Task<Dictionary<(int PrescriptionId, int MedicineId), decimal?>> GetPrescriptionItemPriceMap()
        {
            var items = await _context.PrescriptionItems
                .Select(pi => new { pi.PrescriptionId, pi.MedicineId, pi.UnitPrice })
                .ToListAsync();
            return items
                .GroupBy(x => (x.PrescriptionId, x.MedicineId))
                .ToDictionary(g => g.Key, g => g.First().UnitPrice);
        }

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task<FlashSale> AddFlashSale(FlashSale flashSale)
        {
            _context.FlashSales.Add(flashSale);
            await _context.SaveChangesAsync();
            return flashSale;
        }

        public async Task<FlashSale> GetActiveFlashSaleByMedicine(int medicineId)
        {
            return await _context.FlashSales
                .Where(f => f.MedicineId == medicineId && f.IsActive)
                .OrderByDescending(f => f.AppliedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<FlashSale>> GetFlashSales(bool activeOnly)
        {
            var query = _context.FlashSales
                .Include(f => f.Medicine)
                .Include(f => f.Batch)
                .Include(f => f.AppliedByStaff)
                .AsQueryable();

            if (activeOnly) query = query.Where(f => f.IsActive);

            return await query.OrderByDescending(f => f.AppliedAt).ToListAsync();
        }
    }
}
