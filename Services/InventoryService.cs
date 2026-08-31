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

        // Xuất kho thủ công (vd. dùng nội bộ, hủy không qua quy trình lô) — LUÔN đi qua FEFO để trừ
        // đúng vào (các) lô cụ thể. Trước đây type Adjustment/Export ghi thẳng vào cache InventoryStock/
        // Medicine.StockQuantity mà không đụng StockBatch — cache đó bị RecomputeStockCaches (chạy ở mọi
        // thao tác lô khác) tính lại từ tổng StockBatch ngay sau đó, âm thầm xoá bỏ hiệu lực của thao tác
        // này. Nhập kho (Import) phải qua /api/inventory/batches; điều chỉnh theo kiểm kê phải qua
        // /api/inventory/batches/{id}/adjust (gắn với 1 lô cụ thể) — cả hai không thể map an toàn vào
        // đây vì không có ngữ nghĩa "trừ theo lô nào" cho một mức tồn kho tổng.
        public async Task<InventoryTransactionResponseDTO> CreateTransaction(StockTransactionCreateDTO dto)
        {
            if (dto.Quantity <= 0)
                throw new ArgumentException("Số lượng phải lớn hơn 0.");

            if (dto.Type != "Export")
                throw new ArgumentException("Chỉ hỗ trợ Export qua endpoint này. Nhập kho (Import) phải qua /api/inventory/batches để theo dõi hạn dùng; điều chỉnh theo kiểm kê phải qua /api/inventory/batches/{id}/adjust để không làm sai lệch số liệu theo lô.");

            var referenceId = string.IsNullOrWhiteSpace(dto.ReferenceId) ? "MANUAL-EXPORT" : dto.ReferenceId;
            await DeductStockFEFO(dto.MedicineId, dto.WarehouseId, dto.Quantity, referenceId);

            var list = await _repo.GetTransactions(dto.MedicineId, dto.WarehouseId);
            var latest = list.FirstOrDefault(t => t.Type == "Export" && t.ReferenceId == referenceId);
            return latest == null ? null : MapTransaction(latest);
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
            if (dto.UnitCostPrice.HasValue && dto.UnitCostPrice.Value < 0)
                throw new ArgumentException("Giá nhập không được âm.");

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

                // Giá vốn BÌNH QUÂN GIA QUYỀN theo số lượng còn lại hiện có + số lượng nhập mới,
                // thay vì ghi đè — ghi đè sẽ làm sai giá vốn của số lượng đã nhập từ đợt trước
                // (vốn đã tính lãi/lỗ dựa trên giá cũ) mỗi khi báo cáo lãi gộp đọc lại UnitCostPrice hiện tại.
                if (dto.UnitCostPrice != null)
                {
                    var priorQty = existing.QuantityRemaining;
                    var priorCost = existing.UnitCostPrice ?? dto.UnitCostPrice.Value;
                    var totalQty = priorQty + dto.Quantity;
                    existing.UnitCostPrice = totalQty > 0
                        ? Math.Round((priorQty * priorCost + dto.Quantity * dto.UnitCostPrice.Value) / totalQty, 2)
                        : dto.UnitCostPrice;
                }

                existing.QuantityReceived += dto.Quantity;
                existing.QuantityRemaining += dto.Quantity;
                // Giá bán là quyết định của Dược sĩ cho đợt nhập này, không phải dữ kiện cần bình quân
                // gia quyền như giá vốn — ghi đè trực tiếp khi có truyền lên.
                if (dto.SellPrice != null) existing.SellPrice = dto.SellPrice;
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
                    SellPrice = dto.SellPrice,
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
            if (dto.SellPrice != null)
            {
                await _repo.SyncPriceFromNewBatchIfFrontAsync(dto.MedicineId, batch.Id);
            }

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
            // Khoá dòng (UPDLOCK/ROWLOCK) như DeductStockFEFO/RestoreStockFEFO — tránh 2 nhân viên cùng
            // hủy/điều chỉnh 1 lô đồng thời đọc QuantityRemaining cũ rồi cùng ghi đè (mất cập nhật).
            var tx = await _repo.BeginTransactionAsync();
            try
            {
                var batch = await _repo.GetBatchByIdForUpdate(batchId);
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

            var full = await _repo.GetBatchById(batchId);
            return MapBatch(full);
        }

        public async Task<StockBatchResponseDTO> AdjustBatch(int batchId, BatchAdjustDTO dto)
        {
            var tx = await _repo.BeginTransactionAsync();
            try
            {
                var batch = await _repo.GetBatchByIdForUpdate(batchId);
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
                        // Chốt giá vốn của lô NGAY LÚC xuất — nếu sau này lô được nhập thêm với giá khác
                        // (làm StockBatch.UnitCostPrice đổi), giá vốn của lần bán này vẫn giữ nguyên đúng
                        // thực tế, không bị tính lại khi báo cáo lãi gộp chạy sau đó.
                        UnitCostPrice = batch.UnitCostPrice,
                        CreatedAt = DateTime.Now
                    });
                }

                // Chỉ tính là "bán theo Flash Sale" khi đây là 1 đơn hàng/đơn thuốc thực sự — không tính
                // xuất kho thủ công/nội bộ (vd hao hụt, dùng thử) dù giờ đường đó cũng đi qua FEFO (xem
                // CreateTransaction), nếu không 1 lần xuất nội bộ có thể vô tình làm Flash Sale tự kết
                // thúc sớm (chạm QuantityLimit) dù chưa khách nào mua đủ.
                if (IsCustomerSaleReference(referenceId)) await TrackFlashSaleSold(medicineId, quantity);
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

        private static bool IsCustomerSaleReference(string? referenceId) =>
            !string.IsNullOrEmpty(referenceId) && (referenceId.StartsWith("ORDER-") || referenceId.StartsWith("RX-"));

        // Cộng dồn số lượng đã bán theo giá Flash Sale (nếu có giới hạn số lượng) — dùng để tự động gỡ
        // khi bán hết suất, không cần đợi tick định kỳ. Đồng thời: nếu Flash Sale này được tạo từ 1 lô
        // cận date cụ thể (BatchId) và lô đó vừa bán hết, tự kết thúc sale ngay — Flash Sale được tạo ra
        // để xả đúng lô đó, không phải để tiếp tục giảm giá cho lô kế tiếp (có thể khác giá vốn), tránh
        // sai lệch biên lợi nhuận so với mục đích ban đầu.
        private async Task TrackFlashSaleSold(int medicineId, int quantity)
        {
            var activeSale = await _repo.GetActiveFlashSaleByMedicine(medicineId);
            if (activeSale == null) return;
            if (activeSale.StartTime.HasValue && activeSale.StartTime.Value > DateTime.Now) return;

            if (activeSale.QuantityLimit.HasValue)
                activeSale.QuantitySold += quantity;

            if (activeSale.BatchId.HasValue)
            {
                var batch = await _repo.GetBatchById(activeSale.BatchId.Value);
                if (batch == null || batch.QuantityRemaining <= 0)
                {
                    var medicine = await _repo.GetMedicineById(medicineId);
                    if (medicine != null && medicine.Price == activeSale.SalePrice)
                    {
                        medicine.Price = (medicine.OldPrice.HasValue && medicine.OldPrice > 0) ? medicine.OldPrice : activeSale.OriginalPrice;
                        medicine.OldPrice = null;
                        medicine.Discount = null;
                    }
                    activeSale.IsActive = false;
                    activeSale.RemovedAt = DateTime.UtcNow;
                }
            }
        }

        public async Task RestoreStockFEFO(int medicineId, int warehouseId, int quantity, string referenceId, string? originalExportReferenceId = null)
        {
            if (quantity <= 0) return;

            var tx = await _repo.BeginTransactionAsync();
            try
            {
                var remaining = quantity;

                // Hoàn đúng vào (các) lô đã thực sự xuất cho giao dịch gốc, nếu còn xác định được — chính
                // xác hơn cho lãi/lỗ theo lô so với việc luôn dồn vào lô hết hạn sớm nhất hiện tại (vốn có
                // thể là lô khác với lô đã bán). Ưu tiên dùng originalExportReferenceId do caller truyền
                // vào tường minh (caller luôn biết chính xác ReferenceId gốc vì chính họ đã gọi DeductStockFEFO
                // với giá trị đó) — chỉ suy ra qua quy ước hậu tố "-RESTOCK" khi caller không truyền, để
                // tương thích ngược với các chỗ gọi cũ chưa cập nhật.
                var originalReferenceId = originalExportReferenceId
                    ?? (referenceId.EndsWith("-RESTOCK", StringComparison.Ordinal)
                        ? referenceId[..^"-RESTOCK".Length]
                        : null);

                if (originalReferenceId != null)
                {
                    var originalExports = (await _repo.GetExportTransactionsForReference(medicineId, warehouseId, originalReferenceId))
                        ?? new List<InventoryTransaction>();

                    foreach (var exportTx in originalExports.OrderByDescending(t => t.CreatedAt))
                    {
                        if (remaining <= 0) break;
                        var batch = await _repo.GetBatchByIdForUpdate(exportTx.StockBatchId!.Value);
                        if (batch == null || batch.Status == StockBatchStatus.Disposed) continue;

                        var give = Math.Min(exportTx.Quantity, remaining);
                        batch.QuantityRemaining += give;
                        remaining -= give;

                        await _repo.AddTransaction(new InventoryTransaction
                        {
                            MedicineId = medicineId,
                            WarehouseId = warehouseId,
                            Type = "Import",
                            Quantity = give,
                            ReferenceId = referenceId,
                            StockBatchId = batch.Id,
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                if (remaining > 0)
                {
                    // Không xác định được lô gốc (dữ liệu cũ trước khi có cơ chế này, hoặc lô gốc đã bị
                    // hủy/không còn) — dồn phần còn lại vào lô còn hạn gần nhất hiện có, như trước đây.
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
                            QuantityReceived = remaining,
                            QuantityRemaining = 0,
                            ReceivedAt = DateTime.Now,
                            Status = StockBatchStatus.Active,
                            Note = "Hàng hoàn trả từ đơn hủy — cần Dược sĩ kiểm tra lại hạn dùng thực tế."
                        });
                    }

                    target.QuantityRemaining += remaining;

                    await _repo.AddTransaction(new InventoryTransaction
                    {
                        MedicineId = medicineId,
                        WarehouseId = warehouseId,
                        Type = "Import",
                        Quantity = remaining,
                        ReferenceId = referenceId,
                        StockBatchId = target.Id,
                        CreatedAt = DateTime.Now
                    });
                }

                if (IsCustomerSaleReference(referenceId)) await UntrackFlashSaleSold(medicineId, quantity);
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

        // Ngược lại TrackFlashSaleSold — khi đơn hủy/trả hàng, trừ lại số đã tính là "đã bán theo Flash Sale"
        // nếu sản phẩm vẫn đang trong đợt Flash Sale đó (không hồi lại nếu đợt sale đã kết thúc).
        private async Task UntrackFlashSaleSold(int medicineId, int quantity)
        {
            var activeSale = await _repo.GetActiveFlashSaleByMedicine(medicineId);
            if (activeSale == null || !activeSale.QuantityLimit.HasValue) return;
            activeSale.QuantitySold = Math.Max(0, activeSale.QuantitySold - quantity);
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

            if (dto.StartTime.HasValue && dto.EndTime.HasValue && dto.EndTime <= dto.StartTime)
                throw new ArgumentException("Thời gian kết thúc phải sau thời gian bắt đầu.");
            if (dto.EndTime.HasValue && dto.EndTime <= now)
                throw new ArgumentException("Thời gian kết thúc phải ở tương lai.");
            if (dto.QuantityLimit.HasValue && dto.QuantityLimit <= 0)
                throw new ArgumentException("Giới hạn số lượng bán phải lớn hơn 0.");

            var originalPrice = (medicine.OldPrice != null && medicine.OldPrice > 0) ? medicine.OldPrice.Value : medicine.Price.Value;
            var salePrice = Math.Round(originalPrice * (1 - discountPercent / 100m), 0);
            var startsImmediately = !dto.StartTime.HasValue || dto.StartTime.Value <= now;

            // Kết thúc Flash Sale đang Active hiện có cho sản phẩm này trước khi tạo bản ghi mới — trước
            // đây luôn INSERT mới mà không dọn bản ghi cũ, khiến 2 bản ghi Active tồn tại song song cho
            // cùng 1 sản phẩm (dễ xảy ra vì nút "Đưa vào Flash Sale" ở FE không kiểm tra sản phẩm đã đang
            // sale hay chưa). Bản ghi cũ không có EndTime/QuantityLimit thì mồ côi vĩnh viễn vì
            // RemoveFlashSale chỉ gỡ được bản ghi mới nhất.
            var existingActive = await _repo.GetActiveFlashSaleByMedicine(medicineId);
            if (existingActive != null)
            {
                existingActive.IsActive = false;
                existingActive.RemovedAt = DateTime.UtcNow;
                existingActive.RemovedByStaffId = staffId;
            }

            if (startsImmediately)
            {
                if (medicine.OldPrice == null || medicine.OldPrice <= 0)
                    medicine.OldPrice = medicine.Price;
                medicine.Price = salePrice;
                medicine.Discount = discountPercent;
                await _repo.SaveChangesAsync();
            }

            // PriceApplied = đã áp dụng ngay (startsImmediately) hay chưa (hẹn giờ tương lai, để
            // SweepFlashSales tự áp dụng đúng 1 lần khi tới StartTime — xem SweepFlashSales).
            var priceApplied = startsImmediately;

            // Ghi vào bảng quản lý Flash Sale (lịch sử áp dụng, để Admin theo dõi/quản lý riêng
            // thay vì chỉ suy ra từ Medicine.Discount). Nếu hẹn giờ tương lai, giá thật sự chỉ đổi
            // khi FlashSaleBackgroundService quét tới đúng StartTime.
            await _repo.AddFlashSale(new FlashSale
            {
                MedicineId = medicine.Id,
                BatchId = nearest?.Id,
                OriginalPrice = originalPrice,
                SalePrice = salePrice,
                DiscountPercent = discountPercent,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                QuantityLimit = dto.QuantityLimit,
                QuantitySold = 0,
                BatchExpiryDate = nearest?.ExpiryDate,
                DaysUntilExpiryAtApply = days,
                AppliedAt = DateTime.UtcNow,
                AppliedByStaffId = staffId,
                IsActive = true,
                PriceApplied = priceApplied
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
                IsOnFlashSale = startsImmediately
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
                RemovedAt = f.RemovedAt,
                StartTime = f.StartTime,
                EndTime = f.EndTime,
                QuantityLimit = f.QuantityLimit,
                QuantitySold = f.QuantitySold,
                Status = FlashSaleStatus(f)
            }).ToList();
        }

        public async Task<List<PublicFlashSaleDTO>> GetActiveFlashSalesForCustomer()
        {
            var records = await _repo.GetFlashSales(true);
            return records
                .Where(f => f.Medicine != null)
                .Select(f => new PublicFlashSaleDTO
                {
                    MedicineId = f.MedicineId,
                    MedicineName = f.Medicine.Name,
                    ImageUrl = f.Medicine.ImageUrl,
                    Unit = f.Medicine.Unit,
                    Origin = f.Medicine.Origin,
                    OriginalPrice = f.OriginalPrice,
                    SalePrice = f.SalePrice,
                    DiscountPercent = f.DiscountPercent,
                    StartTime = f.StartTime,
                    EndTime = f.EndTime,
                    QuantityLimit = f.QuantityLimit,
                    QuantitySold = f.QuantitySold,
                    StockQuantity = f.Medicine.StockQuantity,
                    Status = FlashSaleStatus(f)
                })
                .OrderBy(f => f.Status == "Scheduled" ? 1 : 0)
                .ThenBy(f => f.Status == "Running" ? (f.EndTime ?? DateTime.MaxValue) : (f.StartTime ?? DateTime.MaxValue))
                .ToList();
        }

        private static string FlashSaleStatus(FlashSale f)
        {
            if (!f.IsActive) return "Ended";
            var now = DateTime.Now;
            if (f.StartTime.HasValue && f.StartTime.Value > now) return "Scheduled";
            return "Running";
        }

        // Chạy định kỳ từ FlashSaleBackgroundService: kích hoạt các Flash Sale đã hẹn giờ tới đúng
        // StartTime (áp giá sale vào Medicine), và tự gỡ các Flash Sale đã hết EndTime hoặc bán hết
        // suất giới hạn (trả giá về giá gốc), để không cần Admin phải thao tác thủ công đúng giờ.
        public async Task SweepFlashSales()
        {
            var actives = await _repo.GetFlashSales(true);
            var now = DateTime.Now;
            var changed = false;

            foreach (var f in actives)
            {
                var medicine = f.Medicine;
                if (medicine == null) continue;

                var scheduledFuture = f.StartTime.HasValue && f.StartTime.Value > now;
                if (scheduledFuture) continue;

                var expired = (f.EndTime.HasValue && f.EndTime.Value <= now)
                    || (f.QuantityLimit.HasValue && f.QuantitySold >= f.QuantityLimit.Value);

                if (expired)
                {
                    if (medicine.Price == f.SalePrice)
                    {
                        medicine.Price = (medicine.OldPrice.HasValue && medicine.OldPrice > 0) ? medicine.OldPrice : f.OriginalPrice;
                        medicine.OldPrice = null;
                        medicine.Discount = null;
                    }
                    f.IsActive = false;
                    f.RemovedAt = DateTime.UtcNow;
                    changed = true;
                    continue;
                }

                // Chỉ ép giá sale vào Medicine.Price ĐÚNG 1 LẦN — lúc Flash Sale hẹn giờ thực sự bắt đầu
                // (PriceApplied vẫn false vì lúc tạo chưa tới StartTime nên chưa áp dụng ngay). Không được
                // so sánh "Price != SalePrice" rồi ép lại mỗi lần quét như trước đây — nếu không, bất cứ
                // lúc nào Admin/Dược sĩ sửa giá tay cho sản phẩm đang có Flash Sale Active, giá sửa sẽ bị
                // job này (chạy mỗi phút) âm thầm ghi đè trở lại giá sale trong vòng tối đa 1 phút sau.
                if (!f.PriceApplied)
                {
                    if (medicine.OldPrice == null || medicine.OldPrice <= 0)
                        medicine.OldPrice = medicine.Price ?? f.OriginalPrice;
                    medicine.Price = f.SalePrice;
                    medicine.Discount = f.DiscountPercent;
                    f.PriceApplied = true;
                    changed = true;
                }
            }

            if (changed) await _repo.SaveChangesAsync();
        }

        public async Task<List<BatchProfitDTO>> GetBatchProfitReport(int? warehouseId, int? medicineId)
        {
            // Bắt buộc chọn sản phẩm — tránh quét toàn bộ tồn kho khi không cần thiết
            if (!medicineId.HasValue) return new List<BatchProfitDTO>();

            var batches = await _repo.GetBatchesWithCost(warehouseId, medicineId);
            if (batches.Count == 0) return new List<BatchProfitDTO>();

            var batchIds = batches.Select(b => b.Id).ToHashSet();
            // medicineId cố định cho cả report nên giá hiện tại chỉ cần lấy 1 lần, dùng làm fallback cuối cùng.
            var currentSellPrice = batches[0].Medicine?.Price;

            var exportTx = await _repo.GetExportTransactionsWithBatch();
            var orderStatusMap = await _repo.GetOrderStatusMap();
            var orderPriceMap = await _repo.GetOrderItemPriceMap();
            var prescriptionPriceMap = await _repo.GetPrescriptionItemPriceMap();

            var txByBatch = exportTx
                .Where(t => t.StockBatchId.HasValue && batchIds.Contains(t.StockBatchId.Value) && CountsAsSold(t.ReferenceId, orderStatusMap))
                .GroupBy(t => t.StockBatchId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            return batches.Select(b =>
            {
                var txs = txByBatch.GetValueOrDefault(b.Id) ?? new List<InventoryTransaction>();
                var qtySold = txs.Sum(t => t.Quantity);
                var revenue = 0m;
                var cost = 0m;
                var isEstimated = false;
                foreach (var t in txs)
                {
                    var (price, isActual) = ResolveUnitPrice(t.ReferenceId, t.MedicineId, orderPriceMap, prescriptionPriceMap, currentSellPrice ?? 0m);
                    revenue += t.Quantity * price;
                    if (!isActual) isEstimated = true;
                    // Ưu tiên giá vốn đã chốt tại thời điểm xuất (t.UnitCostPrice) — chỉ rơi về giá vốn
                    // HIỆN TẠI của lô cho các giao dịch cũ tạo trước khi có cột snapshot này, và đánh dấu
                    // IsEstimated để FE biết con số này không hoàn toàn chính xác (giá vốn lúc bán thật đã
                    // mất, không thể phục hồi).
                    if (t.UnitCostPrice == null) isEstimated = true;
                    cost += t.Quantity * (t.UnitCostPrice ?? b.UnitCostPrice ?? 0m);
                }
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
                    CurrentSellPrice = currentSellPrice,
                    EstimatedRevenue = revenue,
                    EstimatedCost = cost,
                    EstimatedGrossProfit = profit,
                    GrossMarginPercent = revenue > 0 ? Math.Round(profit / revenue * 100, 1) : null,
                    IsEstimated = isEstimated
                };
            })
            .OrderByDescending(p => p.EstimatedGrossProfit)
            .ToList();
        }

        // Như GetBatchProfitReport nhưng gộp lãi gộp của MỌI sản phẩm theo kỳ (ngày/tháng/năm) — cho
        // chủ cửa hàng xem "tháng này lãi gộp bao nhiêu" trong 1 màn hình thay vì phải chọn từng sản
        // phẩm một. Dùng chung quy tắc "đã bán"/giá bán thực tế với GetBatchProfitReport.
        public async Task<List<ProfitPointDTO>> GetProfitByPeriod(DateTime from, DateTime to, string groupBy)
        {
            var exportTx = await _repo.GetExportTransactionsWithBatchInRange(from, to);
            if (exportTx.Count == 0) return new List<ProfitPointDTO>();

            var orderStatusMap = await _repo.GetOrderStatusMap();
            var orderPriceMap = await _repo.GetOrderItemPriceMap();
            var prescriptionPriceMap = await _repo.GetPrescriptionItemPriceMap();

            var buckets = new Dictionary<string, ProfitPointDTO>();
            foreach (var t in exportTx)
            {
                // Ưu tiên giá vốn đã chốt tại thời điểm xuất (t.UnitCostPrice) — chỉ rơi về giá vốn HIỆN
                // TẠI của lô cho các giao dịch cũ tạo trước khi có cột snapshot này.
                var unitCost = t.UnitCostPrice ?? t.StockBatch?.UnitCostPrice;
                if (unitCost == null) continue;
                if (!CountsAsSold(t.ReferenceId, orderStatusMap)) continue;

                var key = DateKey(t.CreatedAt, groupBy);
                if (!buckets.TryGetValue(key, out var point))
                {
                    point = new ProfitPointDTO { Period = key };
                    buckets[key] = point;
                }

                var (price, isActual) = ResolveUnitPrice(t.ReferenceId, t.MedicineId, orderPriceMap, prescriptionPriceMap, t.Medicine?.Price ?? 0m);
                point.EstimatedRevenue += t.Quantity * price;
                point.EstimatedCost += t.Quantity * unitCost.Value;
                // Đánh dấu ước tính khi giá bán KHÔNG chốt được (isActual=false) hoặc giá vốn không có
                // snapshot (t.UnitCostPrice null, phải rơi về giá vốn hiện tại của lô).
                if (!isActual || t.UnitCostPrice == null) point.IsEstimated = true;
            }

            foreach (var point in buckets.Values)
            {
                point.EstimatedGrossProfit = point.EstimatedRevenue - point.EstimatedCost;
                point.GrossMarginPercent = point.EstimatedRevenue > 0
                    ? Math.Round(point.EstimatedGrossProfit / point.EstimatedRevenue * 100, 1)
                    : null;
            }

            return buckets.Values.OrderBy(p => p.Period).ToList();
        }

        // Gợi ý nhập hàng: với mỗi sản phẩm có bán ra trong lookbackDays ngày gần nhất, ước tính tốc độ
        // bán trung bình/ngày, nhân với leadTimeDays (thời gian dự kiến chờ hàng về) để ra "cần có bao
        // nhiêu hàng cho tới khi lô mới về", trừ đi tồn kho hiện có — dương thì gợi ý nhập thêm đúng số
        // đó. Chỉ là ước tính tham khảo (không tính mùa vụ/khuyến mãi sắp tới), sản phẩm chưa từng bán
        // trong khoảng lookback thì không có cơ sở tính tốc độ bán nên không đưa vào gợi ý.
        public async Task<List<ReorderSuggestionDTO>> GetReorderSuggestions(int lookbackDays, int leadTimeDays)
        {
            if (lookbackDays <= 0) lookbackDays = 30;
            if (leadTimeDays <= 0) leadTimeDays = 30;

            var from = DateTime.Now.AddDays(-lookbackDays);
            var to = DateTime.Now;
            var exportTx = await _repo.GetExportTransactionsWithBatchInRange(from, to);
            if (exportTx.Count == 0) return new List<ReorderSuggestionDTO>();

            var orderStatusMap = await _repo.GetOrderStatusMap();

            var soldByMedicine = exportTx
                .Where(t => CountsAsSold(t.ReferenceId, orderStatusMap))
                .GroupBy(t => t.MedicineId)
                .ToDictionary(g => g.Key, g => new { Qty = g.Sum(t => t.Quantity), Name = g.First().Medicine?.Name });

            if (soldByMedicine.Count == 0) return new List<ReorderSuggestionDTO>();

            var allStock = await _repo.GetAllStock();
            var stockByMedicine = allStock.GroupBy(s => s.MedicineId).ToDictionary(g => g.Key, g => g.Sum(s => s.Quantity));

            var suggestions = new List<ReorderSuggestionDTO>();
            foreach (var pair in soldByMedicine)
            {
                var avgDailySales = (decimal)pair.Value.Qty / lookbackDays;
                var currentStock = stockByMedicine.GetValueOrDefault(pair.Key);
                var neededForLeadTime = (int)Math.Ceiling(avgDailySales * leadTimeDays);
                var suggestedQty = neededForLeadTime - currentStock;
                if (suggestedQty <= 0) continue;

                suggestions.Add(new ReorderSuggestionDTO
                {
                    MedicineId = pair.Key,
                    MedicineName = pair.Value.Name,
                    CurrentStock = currentStock,
                    AvgDailySales = Math.Round(avgDailySales, 2),
                    LeadTimeDays = leadTimeDays,
                    SuggestedReorderQuantity = suggestedQty
                });
            }

            return suggestions.OrderByDescending(s => s.SuggestedReorderQuantity).ToList();
        }

        private static string DateKey(DateTime d, string groupBy) => groupBy switch
        {
            "Month" => d.ToString("yyyy-MM"),
            "Year" => d.ToString("yyyy"),
            _ => d.ToString("yyyy-MM-dd")
        };

        // Chỉ tính là "đã bán thực tế" khi đơn hàng đã thanh toán (đã nhận tiền) và không bị hủy/trả
        // hàng. Đơn kê thuốc (RX-*) không có khái niệm thanh toán riêng trong hệ thống này nên tạm tính
        // là đã bán ngay khi kê đơn. Dùng chung cho GetBatchProfitReport và GetProfitByPeriod.
        private static bool CountsAsSold(string? referenceId, Dictionary<int, (string? Status, string? PaymentStatus)> orderStatusMap)
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

        // Giá bán THỰC TẾ tại thời điểm bán: OrderItem.Price (đơn hàng) hoặc PrescriptionItem.UnitPrice
        // (đơn thuốc, snapshot từ Medicine.Price lúc kê/duyệt đơn). Chỉ rơi về giá fallback (giá hiện tại
        // của Medicine) khi không tra được giá thực — dữ liệu tạo trước khi có snapshot giá đơn thuốc.
        private static (decimal Price, bool IsActual) ResolveUnitPrice(
            string referenceId, int medId,
            Dictionary<(int OrderId, int MedicineId), decimal> orderPriceMap,
            Dictionary<(int PrescriptionId, int MedicineId), decimal?> prescriptionPriceMap,
            decimal fallbackPrice)
        {
            if (referenceId.StartsWith("ORDER-") && int.TryParse(referenceId.AsSpan(6), out var orderId)
                && orderPriceMap.TryGetValue((orderId, medId), out var orderPrice))
            {
                return (orderPrice, true);
            }
            if (referenceId.StartsWith("RX-") && int.TryParse(referenceId.AsSpan(3), out var rxId)
                && prescriptionPriceMap.TryGetValue((rxId, medId), out var rxPrice) && rxPrice.HasValue)
            {
                return (rxPrice.Value, true);
            }
            return (fallbackPrice, false);
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
            SellPrice = b.SellPrice,
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
