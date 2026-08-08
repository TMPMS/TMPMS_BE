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

            var allowedTypes = new[] { "Export", "Adjustment" };
            if (!allowedTypes.Contains(dto.Type))
                throw new ArgumentException("Loại giao dịch không hợp lệ (Export/Adjustment). Nhập kho (Import) phải thực hiện qua API tạo lô hàng (/api/inventory/batches) để theo dõi hạn dùng.");

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
            var batches = await _repo.GetBatchesExpiringWithin(daysAhead);
            return batches.Select(b => new ExpiryAlertDTO
            {
                BatchId = b.Id,
                MedicineId = b.MedicineId,
                MedicineName = b.Medicine?.Name,
                WarehouseId = b.WarehouseId,
                WarehouseName = b.Warehouse?.Name,
                BatchNumber = b.BatchNumber,
                ExpiryDate = b.ExpiryDate,
                DaysRemaining = (b.ExpiryDate.Date - DateTime.Now.Date).Days,
                QuantityRemaining = b.QuantityRemaining,
                Severity = Severity(b.ExpiryDate)
            }).ToList();
        }

        private static string Severity(DateTime expiryDate)
        {
            var days = (expiryDate.Date - DateTime.Now.Date).Days;
            if (days < 0) return "Expired";
            if (days <= 7) return "Critical";
            if (days <= 30) return "Warning";
            return "Notice";
        }

        private static int SuggestedDiscountPercent(int daysRemaining)
        {
            if (daysRemaining <= 30) return 30;
            if (daysRemaining <= 90) return 20;
            return 10;
        }

        public async Task<StockBatchResponseDTO> CreateBatch(StockBatchCreateDTO dto)
        {
            if (dto.Quantity <= 0)
                throw new ArgumentException("Số lượng nhập phải lớn hơn 0.");
            if (dto.ExpiryDate.Date <= dto.ManufactureDate.Date)
                throw new ArgumentException("Hạn sử dụng phải sau ngày sản xuất.");
            if (dto.ExpiryDate.Date < DateTime.Now.Date)
                throw new ArgumentException("Không thể nhập lô hàng đã hết hạn sử dụng.");

            var medicine = await _repo.GetMedicineById(dto.MedicineId);
            if (medicine == null)
                throw new ArgumentException("Không tìm thấy thuốc/dược liệu.");

            var batchNumber = string.IsNullOrWhiteSpace(dto.BatchNumber)
                ? $"LOT-{dto.MedicineId}-{DateTime.Now:yyyyMMddHHmmss}"
                : dto.BatchNumber.Trim();

            var existing = await _repo.GetBatchByNumber(dto.MedicineId, dto.WarehouseId, batchNumber);
            StockBatch batch;
            if (existing != null)
            {
                if (existing.ExpiryDate.Date != dto.ExpiryDate.Date || existing.ManufactureDate.Date != dto.ManufactureDate.Date)
                    throw new ArgumentException($"Số lô '{batchNumber}' đã tồn tại với NSX/HSD khác. Vui lòng dùng số lô khác cho đợt nhập này.");

                existing.QuantityReceived += dto.Quantity;
                existing.QuantityRemaining += dto.Quantity;
                if (dto.UnitCostPrice != null) existing.UnitCostPrice = dto.UnitCostPrice;
                batch = existing;
            }
            else
            {
                var initialStatus = (dto.QcStatus == "Fail" || dto.QcStatus == "Quarantine")
                    ? StockBatchStatus.Quarantine
                    : StockBatchStatus.Active;

                var combinedNote = string.Join(" | ", new[]
                {
                    !string.IsNullOrWhiteSpace(dto.RegistrationNumber) ? $"SĐK: {dto.RegistrationNumber}" : null,
                    !string.IsNullOrWhiteSpace(dto.StorageCondition) ? $"Bảo quản: {dto.StorageCondition}" : null,
                    dto.Note
                }.Where(s => !string.IsNullOrWhiteSpace(s)));

                batch = new StockBatch
                {
                    MedicineId = dto.MedicineId,
                    WarehouseId = dto.WarehouseId,
                    SupplierId = dto.SupplierId,
                    BatchNumber = batchNumber,
                    ManufactureDate = dto.ManufactureDate,
                    ExpiryDate = dto.ExpiryDate,
                    QuantityReceived = dto.Quantity,
                    QuantityRemaining = dto.Quantity,
                    UnitCostPrice = dto.UnitCostPrice,
                    ReceivedAt = DateTime.Now,
                    Status = initialStatus,
                    Note = string.IsNullOrWhiteSpace(combinedNote) ? null : combinedNote
                };
                batch = await _repo.AddBatch(batch);
            }

            await _repo.AddTransaction(new InventoryTransaction
            {
                MedicineId = dto.MedicineId,
                WarehouseId = dto.WarehouseId,
                Type = "Import",
                Quantity = dto.Quantity,
                ReferenceId = $"BATCH-{batchNumber}",
                StockBatchId = batch.Id,
                CreatedAt = DateTime.Now
            });

            await _repo.RecomputeStockCaches(dto.MedicineId, dto.WarehouseId);

            var full = await _repo.GetBatchById(batch.Id);
            return MapBatch(full);
        }

        public async Task<List<StockBatchResponseDTO>> GetBatchesByMedicine(int medicineId, int? warehouseId)
        {
            var batches = await _repo.GetBatchesByMedicine(medicineId, warehouseId);
            return batches.Select(MapBatch).ToList();
        }

        public async Task<List<StockBatchResponseDTO>> GetBatchesByWarehouse(int warehouseId)
        {
            var batches = await _repo.GetBatchesByWarehouse(warehouseId);
            return batches.Select(MapBatch).ToList();
        }

        public async Task<StockBatchResponseDTO> DisposeBatch(int batchId, BatchDisposeDTO dto)
        {
            var batch = await _repo.GetBatchById(batchId);
            if (batch == null) throw new ArgumentException("Không tìm thấy lô hàng.");

            var qty = dto.Quantity ?? batch.QuantityRemaining;
            if (qty <= 0 || qty > batch.QuantityRemaining)
                throw new ArgumentException($"Số lượng hủy không hợp lệ (còn lại: {batch.QuantityRemaining}).");

            batch.QuantityRemaining -= qty;
            if (batch.QuantityRemaining == 0) batch.Status = StockBatchStatus.Disposed;

            await _repo.AddTransaction(new InventoryTransaction
            {
                MedicineId = batch.MedicineId,
                WarehouseId = batch.WarehouseId,
                Type = "Dispose",
                Quantity = qty,
                ReferenceId = string.IsNullOrWhiteSpace(dto.Reason) ? "Hủy hàng hết hạn" : dto.Reason,
                StockBatchId = batch.Id,
                CreatedAt = DateTime.Now
            });

            await _repo.RecomputeStockCaches(batch.MedicineId, batch.WarehouseId);

            var full = await _repo.GetBatchById(batchId);
            return MapBatch(full);
        }

        public async Task<StockBatchResponseDTO> AdjustBatch(int batchId, BatchAdjustDTO dto)
        {
            var batch = await _repo.GetBatchById(batchId);
            if (batch == null) throw new ArgumentException("Không tìm thấy lô hàng.");
            if (dto.QuantityRemaining < 0)
                throw new ArgumentException("Số lượng kiểm kê không hợp lệ.");

            var diff = dto.QuantityRemaining - batch.QuantityRemaining;
            batch.QuantityRemaining = dto.QuantityRemaining;

            if (diff != 0)
            {
                await _repo.AddTransaction(new InventoryTransaction
                {
                    MedicineId = batch.MedicineId,
                    WarehouseId = batch.WarehouseId,
                    Type = "Adjustment",
                    Quantity = diff,
                    ReferenceId = string.IsNullOrWhiteSpace(dto.Reason) ? "Kiểm kê điều chỉnh" : dto.Reason,
                    StockBatchId = batch.Id,
                    CreatedAt = DateTime.Now
                });
            }

            await _repo.RecomputeStockCaches(batch.MedicineId, batch.WarehouseId);

            var full = await _repo.GetBatchById(batchId);
            return MapBatch(full);
        }

        public async Task DeductStockFEFO(int medicineId, int warehouseId, int quantity, string referenceId)
        {
            if (quantity <= 0) throw new ArgumentException("Số lượng xuất phải lớn hơn 0.");

            // Khoá các lô liên quan (UPDLOCK/ROWLOCK) trong 1 transaction DB thật, để hai request
            // trừ kho đồng thời (2 đơn hàng/đơn thuốc cùng lúc) không cùng đọc số dư cũ rồi cùng
            // ghi đè, tránh bán vượt tồn kho thực tế. Nếu caller (PrescriptionService/OrdersController)
            // đã tự mở transaction bao ngoài thì dùng luôn transaction đó (không mở lồng nhau).
            var tx = await _repo.BeginTransactionAsync();
            try
            {
                var batches = await _repo.GetActiveBatchesForFEFOForUpdate(medicineId, warehouseId);
                var available = batches.Sum(b => b.QuantityRemaining);
                if (available < quantity)
                    throw new InvalidOperationException($"Không đủ tồn kho còn hạn sử dụng (còn {available}, cần {quantity}).");

                var remaining = quantity;
                foreach (var batch in batches)
                {
                    if (remaining <= 0) break;
                    var take = Math.Min(batch.QuantityRemaining, remaining);
                    batch.QuantityRemaining -= take;
                    remaining -= take;

                    await _repo.AddTransaction(new InventoryTransaction
                    {
                        MedicineId = medicineId,
                        WarehouseId = warehouseId,
                        Type = "Export",
                        Quantity = take,
                        ReferenceId = referenceId,
                        StockBatchId = batch.Id,
                        CreatedAt = DateTime.Now
                    });
                }

                await _repo.RecomputeStockCaches(medicineId, warehouseId);
                if (tx != null) await tx.CommitAsync();
            }
            catch
            {
                if (tx != null) await tx.RollbackAsync();
                throw;
            }
            finally
            {
                if (tx != null) tx.Dispose();
            }
        }

        public async Task RestoreStockFEFO(int medicineId, int warehouseId, int quantity, string referenceId)
        {
            if (quantity <= 0) return;

            var tx = await _repo.BeginTransactionAsync();
            try
            {
                var batches = await _repo.GetActiveBatchesForFEFOForUpdate(medicineId, warehouseId);
                var target = batches.FirstOrDefault();

                if (target == null)
                {
                    // Không còn lô nào đang hoạt động (đã hết hạn/bị hủy hết) — tạo lô "hàng hoàn trả"
                    // để không làm mất số lượng, đánh dấu để Dược sĩ kiểm tra lại hạn dùng thực tế.
                    target = await _repo.AddBatch(new StockBatch
                    {
                        MedicineId = medicineId,
                        WarehouseId = warehouseId,
                        BatchNumber = $"RESTORE-{medicineId}-{DateTime.Now:yyyyMMddHHmmssfff}",
                        ManufactureDate = DateTime.Now,
                        ExpiryDate = DateTime.Now.AddYears(1),
                        QuantityReceived = quantity,
                        QuantityRemaining = 0,
                        ReceivedAt = DateTime.Now,
                        Status = StockBatchStatus.Active,
                        Note = "Hàng hoàn trả từ đơn hủy — cần Dược sĩ kiểm tra lại hạn dùng thực tế."
                    });
                }

                target.QuantityRemaining += quantity;

                await _repo.AddTransaction(new InventoryTransaction
                {
                    MedicineId = medicineId,
                    WarehouseId = warehouseId,
                    Type = "Import",
                    Quantity = quantity,
                    ReferenceId = referenceId,
                    StockBatchId = target.Id,
                    CreatedAt = DateTime.Now
                });

                await _repo.RecomputeStockCaches(medicineId, warehouseId);
                if (tx != null) await tx.CommitAsync();
            }
            catch
            {
                if (tx != null) await tx.RollbackAsync();
                throw;
            }
            finally
            {
                if (tx != null) tx.Dispose();
            }
        }

        public async Task<List<FlashSaleCandidateDTO>> GetFlashSaleCandidates(int daysThreshold)
        {
            var batches = await _repo.GetBatchesExpiringWithin(daysThreshold);
            var now = DateTime.Now;

            return batches
                .Where(b => b.Status == StockBatchStatus.Active && b.QuantityRemaining > 0 && b.ExpiryDate.Date >= now.Date && b.Medicine != null)
                .GroupBy(b => b.MedicineId)
                .Select(g => g.OrderBy(b => b.ExpiryDate).First())
                .OrderBy(b => b.ExpiryDate)
                .Select(b =>
                {
                    var days = (b.ExpiryDate.Date - now.Date).Days;
                    return new FlashSaleCandidateDTO
                    {
                        MedicineId = b.MedicineId,
                        MedicineName = b.Medicine.Name,
                        ImageUrl = b.Medicine.ImageUrl,
                        Price = b.Medicine.Price,
                        OldPrice = b.Medicine.OldPrice,
                        Discount = b.Medicine.Discount,
                        Unit = b.Medicine.Unit,
                        Origin = b.Medicine.Origin,
                        BatchId = b.Id,
                        BatchNumber = b.BatchNumber,
                        NearestExpiryDate = b.ExpiryDate,
                        DaysUntilExpiry = days,
                        QuantityRemaining = b.QuantityRemaining,
                        SuggestedDiscountPercent = SuggestedDiscountPercent(days),
                        IsOnFlashSale = b.Medicine.Discount.HasValue && b.Medicine.Discount > 0
                    };
                })
                .ToList();
        }

        public async Task<FlashSaleCandidateDTO> ApplyFlashSale(int medicineId, ApplyFlashSaleDTO dto, int? staffId)
        {
            var medicine = await _repo.GetMedicineById(medicineId);
            if (medicine == null) throw new ArgumentException("Không tìm thấy thuốc/dược liệu.");
            if (medicine.Price == null) throw new InvalidOperationException("Sản phẩm chưa có giá bán, không thể áp dụng Flash Sale.");

            var batches = await _repo.GetBatchesByMedicine(medicineId, null);
            var now = DateTime.Now;
            var nearest = batches
                .Where(b => b.Status == StockBatchStatus.Active && b.QuantityRemaining > 0 && b.ExpiryDate.Date >= now.Date)
                .OrderBy(b => b.ExpiryDate)
                .FirstOrDefault();

            var days = nearest != null ? (nearest.ExpiryDate.Date - now.Date).Days : (int?)null;
            var discountPercent = dto.DiscountPercent ?? (days.HasValue
                ? SuggestedDiscountPercent(days.Value)
                : throw new InvalidOperationException("Không có lô hàng sắp hết hạn để đề xuất mức giảm giá, vui lòng nhập % giảm giá thủ công."));

            if (discountPercent <= 0 || discountPercent >= 100)
                throw new ArgumentException("Phần trăm giảm giá phải trong khoảng 1-99.");

            if (medicine.OldPrice == null || medicine.OldPrice <= 0)
                medicine.OldPrice = medicine.Price;

            medicine.Price = Math.Round(medicine.OldPrice.Value * (1 - discountPercent / 100m), 0);
            medicine.Discount = discountPercent;

            await _repo.SaveChangesAsync();

            // Ghi vào bảng quản lý Flash Sale (lịch sử áp dụng, để Admin theo dõi/quản lý riêng
            // thay vì chỉ suy ra từ Medicine.Discount).
            await _repo.AddFlashSale(new FlashSale
            {
                MedicineId = medicine.Id,
                BatchId = nearest?.Id,
                OriginalPrice = medicine.OldPrice.Value,
                SalePrice = medicine.Price.Value,
                DiscountPercent = discountPercent,
                BatchExpiryDate = nearest?.ExpiryDate,
                DaysUntilExpiryAtApply = days,
                AppliedAt = DateTime.UtcNow,
                AppliedByStaffId = staffId,
                IsActive = true
            });

            return new FlashSaleCandidateDTO
            {
                MedicineId = medicine.Id,
                MedicineName = medicine.Name,
                ImageUrl = medicine.ImageUrl,
                Price = medicine.Price,
                OldPrice = medicine.OldPrice,
                Discount = medicine.Discount,
                Unit = medicine.Unit,
                Origin = medicine.Origin,
                BatchId = nearest?.Id ?? 0,
                BatchNumber = nearest?.BatchNumber,
                NearestExpiryDate = nearest?.ExpiryDate ?? default,
                DaysUntilExpiry = days ?? 0,
                QuantityRemaining = nearest?.QuantityRemaining ?? 0,
                SuggestedDiscountPercent = discountPercent,
                IsOnFlashSale = true
            };
        }

        public async Task RemoveFlashSale(int medicineId, int? staffId)
        {
            var medicine = await _repo.GetMedicineById(medicineId);
            if (medicine == null) throw new ArgumentException("Không tìm thấy thuốc/dược liệu.");

            if (medicine.OldPrice != null && medicine.OldPrice > 0)
                medicine.Price = medicine.OldPrice;

            medicine.OldPrice = null;
            medicine.Discount = null;

            var active = await _repo.GetActiveFlashSaleByMedicine(medicineId);
            if (active != null)
            {
                active.IsActive = false;
                active.RemovedAt = DateTime.UtcNow;
                active.RemovedByStaffId = staffId;
            }

            await _repo.SaveChangesAsync();
        }

        public async Task<List<FlashSaleRecordDTO>> GetFlashSales(bool activeOnly)
        {
            var records = await _repo.GetFlashSales(activeOnly);
            return records.Select(f => new FlashSaleRecordDTO
            {
                Id = f.Id,
                MedicineId = f.MedicineId,
                MedicineName = f.Medicine?.Name,
                ImageUrl = f.Medicine?.ImageUrl,
                OriginalPrice = f.OriginalPrice,
                SalePrice = f.SalePrice,
                DiscountPercent = f.DiscountPercent,
                BatchNumber = f.Batch?.BatchNumber,
                BatchExpiryDate = f.BatchExpiryDate,
                DaysUntilExpiryAtApply = f.DaysUntilExpiryAtApply,
                AppliedAt = f.AppliedAt,
                AppliedByStaffName = f.AppliedByStaff?.FullName ?? f.AppliedByStaff?.UserName,
                IsActive = f.IsActive,
                RemovedAt = f.RemovedAt
            }).ToList();
        }

        public async Task<List<BatchProfitDTO>> GetBatchProfitReport(int? warehouseId, int? medicineId)
        {
            // Bắt buộc chọn sản phẩm — tránh quét toàn bộ tồn kho khi không cần thiết
            if (!medicineId.HasValue) return new List<BatchProfitDTO>();

            var batches = await _repo.GetBatchesWithCost(warehouseId, medicineId);
            if (batches.Count == 0) return new List<BatchProfitDTO>();

            var exportTx = await _repo.GetExportTransactionsWithBatch();
            var orderStatusMap = await _repo.GetOrderStatusMap();

            // Chỉ tính là "đã bán thực tế" khi đơn hàng đã thanh toán (đã nhận tiền)
            // và không bị hủy/trả hàng. Đơn kê thuốc (RX-*) không có khái niệm thanh toán
            // riêng trong hệ thống này nên tạm tính là đã bán ngay khi kê đơn.
            bool CountsAsSold(string? referenceId)
            {
                if (string.IsNullOrEmpty(referenceId)) return false;
                if (referenceId.StartsWith("ORDER-") && !referenceId.EndsWith("-RESTOCK"))
                {
                    if (int.TryParse(referenceId.AsSpan(6), out var orderId)
                        && orderStatusMap.TryGetValue(orderId, out var info))
                    {
                        return info.PaymentStatus == "Paid" && info.Status != "Cancelled" && info.Status != "Returned";
                    }
                    return false;
                }
                return referenceId.StartsWith("RX-");
            }

            var soldQuantities = exportTx
                .Where(t => CountsAsSold(t.ReferenceId))
                .GroupBy(t => t.StockBatchId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Quantity));

            return batches.Select(b =>
            {
                var qtySold = soldQuantities.GetValueOrDefault(b.Id, 0);
                var sellPrice = b.Medicine?.Price;
                var revenue = qtySold * (sellPrice ?? 0);
                var cost = qtySold * b.UnitCostPrice!.Value;
                var profit = revenue - cost;

                return new BatchProfitDTO
                {
                    BatchId = b.Id,
                    MedicineId = b.MedicineId,
                    MedicineName = b.Medicine?.Name,
                    BatchNumber = b.BatchNumber,
                    WarehouseId = b.WarehouseId,
                    WarehouseName = b.Warehouse?.Name,
                    QuantitySold = qtySold,
                    UnitCostPrice = b.UnitCostPrice.Value,
                    CurrentSellPrice = sellPrice,
                    EstimatedRevenue = revenue,
                    EstimatedCost = cost,
                    EstimatedGrossProfit = profit,
                    GrossMarginPercent = revenue > 0 ? Math.Round(profit / revenue * 100, 1) : null
                };
            })
            .OrderByDescending(p => p.EstimatedGrossProfit)
            .ToList();
        }

        private StockBatchResponseDTO MapBatch(StockBatch b) => new StockBatchResponseDTO
        {
            Id = b.Id,
            MedicineId = b.MedicineId,
            MedicineName = b.Medicine?.Name,
            WarehouseId = b.WarehouseId,
            WarehouseName = b.Warehouse?.Name,
            BatchNumber = b.BatchNumber,
            ManufactureDate = b.ManufactureDate,
            ExpiryDate = b.ExpiryDate,
            QuantityReceived = b.QuantityReceived,
            QuantityRemaining = b.QuantityRemaining,
            UnitCostPrice = b.UnitCostPrice,
            SupplierId = b.SupplierId,
            SupplierName = b.Supplier?.CompanyName,
            ReceivedAt = b.ReceivedAt,
            Status = ComputeDisplayStatus(b),
            DaysUntilExpiry = (b.ExpiryDate.Date - DateTime.Now.Date).Days,
            Note = b.Note
        };

        private static string ComputeDisplayStatus(StockBatch b)
        {
            if (b.Status == StockBatchStatus.Disposed) return StockBatchStatus.Disposed;
            if (b.QuantityRemaining <= 0) return StockBatchStatus.Depleted;
            if (b.ExpiryDate.Date < DateTime.Now.Date) return StockBatchStatus.Expired;
            return StockBatchStatus.Active;
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
