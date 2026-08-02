using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using TMPMS.Data;

namespace TMPMS.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly TMPMSDbContext _context;
        public InventoryRepository(TMPMSDbContext context) => _context = context;

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

        public async Task<List<Medicine>> GetLowStockMedicines(int threshold)
        {
            return await _context.Medicines
                .Where(m => m.StockQuantity <= threshold)
                .OrderBy(m => m.StockQuantity)
                .ToListAsync();
        }

        public async Task<List<Medicine>> GetExpiringMedicines(int daysAhead)
        {
            var limitDate = DateTime.Now.AddDays(daysAhead);
            return await _context.Medicines
                .Where(m => m.ExpiryDate <= limitDate && m.ExpiryDate >= DateTime.Now)
                .OrderBy(m => m.ExpiryDate)
                .ToListAsync();
        }
    }
}
