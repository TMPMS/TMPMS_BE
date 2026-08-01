using BusinessObjects;
using Repositories.Interfaces;
using Services.Interfaces;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _repo;
        public InventoryService(IInventoryRepository repo) => _repo = repo;

        // Nhập/Xuất kho: ghi log transaction + cập nhật số lượng tồn kho tại warehouse
        public async Task<InventoryTransactionResponseDTO> CreateTransaction(StockTransactionCreateDTO dto)
        {
            if (dto.Quantity <= 0)
                throw new ArgumentException("Số lượng phải lớn hơn 0.");

            var allowedTypes = new[] { "Import", "Export", "Adjustment" };
            if (!allowedTypes.Contains(dto.Type))
                throw new ArgumentException("Loại giao dịch không hợp lệ (Import/Export/Adjustment).");

            var stock = await _repo.GetStock(dto.MedicineId, dto.WarehouseId);
            int currentQty = stock?.Quantity ?? 0;

            int newQty = dto.Type switch
            {
                "Import" => currentQty + dto.Quantity,
                "Export" => currentQty - dto.Quantity,
                "Adjustment" => dto.Quantity,
                _ => currentQty
            };

            if (newQty < 0)
                throw new InvalidOperationException("Số lượng tồn kho không đủ để xuất.");

            await _repo.UpsertStock(new InventoryStock
            {
                MedicineId = dto.MedicineId,
                WarehouseId = dto.WarehouseId,
                Quantity = newQty
            });

            var transaction = await _repo.AddTransaction(new InventoryTransaction
            {
                MedicineId = dto.MedicineId,
                WarehouseId = dto.WarehouseId,
                Type = dto.Type,
                Quantity = dto.Quantity,
                ReferenceId = dto.ReferenceId,
                CreatedAt = DateTime.Now
            });

            var list = await _repo.GetTransactions(dto.MedicineId, dto.WarehouseId);
            var full = list.FirstOrDefault(t => t.Id == transaction.Id) ?? transaction;
            return MapTransaction(full);
        }

        public async Task<List<InventoryStockResponseDTO>> GetStockByWarehouse(int warehouseId)
        {
            var list = await _repo.GetStockByWarehouse(warehouseId);
            return list.Select(MapStock).ToList();
        }

        public async Task<List<InventoryStockResponseDTO>> GetStockByMedicine(int medicineId)
        {
            var list = await _repo.GetStockByMedicine(medicineId);
            return list.Select(MapStock).ToList();
        }

        public async Task<List<InventoryStockResponseDTO>> GetAllStock()
        {
            var list = await _repo.GetAllStock();
            return list.Select(MapStock).ToList();
        }

        public async Task<List<InventoryTransactionResponseDTO>> GetTransactions(int? medicineId, int? warehouseId)
        {
            var list = await _repo.GetTransactions(medicineId, warehouseId);
            return list.Select(MapTransaction).ToList();
        }

        public async Task<List<LowStockAlertDTO>> GetLowStockAlerts(int threshold)
        {
            var stocks = await _repo.GetAllStock();
            return stocks.Where(s => s.Quantity <= threshold)
                .Select(s => new LowStockAlertDTO
                {
                    MedicineId = s.MedicineId,
                    MedicineName = s.Medicine?.Name,
                    WarehouseId = s.WarehouseId,
                    WarehouseName = s.Warehouse?.Name,
                    CurrentQuantity = s.Quantity,
                    Threshold = threshold
                }).ToList();
        }

        public async Task<List<ExpiryAlertDTO>> GetExpiryAlerts(int daysAhead)
        {
            var medicines = await _repo.GetExpiringMedicines(daysAhead);
            return medicines.Select(m => new ExpiryAlertDTO
            {
                MedicineId = m.Id,
                MedicineName = m.Name,
                ExpiryDate = m.ExpiryDate,
                DaysRemaining = (m.ExpiryDate - DateTime.Now).Days,
                StockQuantity = m.StockQuantity
            }).ToList();
        }

        private InventoryStockResponseDTO MapStock(InventoryStock s) => new InventoryStockResponseDTO
        {
            MedicineId = s.MedicineId,
            MedicineName = s.Medicine?.Name,
            WarehouseId = s.WarehouseId,
            WarehouseName = s.Warehouse?.Name,
            Quantity = s.Quantity
        };

        private InventoryTransactionResponseDTO MapTransaction(InventoryTransaction t) => new InventoryTransactionResponseDTO
        {
            Id = t.Id,
            MedicineId = t.MedicineId,
            MedicineName = t.Medicine?.Name,
            WarehouseId = t.WarehouseId,
            WarehouseName = t.Warehouse?.Name,
            Type = t.Type,
            Quantity = t.Quantity,
            ReferenceId = t.ReferenceId,
            CreatedAt = t.CreatedAt
        };
    }
}
