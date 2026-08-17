using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Services.Interfaces;
using TMPMS.Data;
using TMPMS.DTOs;
using TMPMS.Utils;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/admin/products")]
    [Route("admin/products")]
    public class ProductImportController : ControllerBase
    {
        private readonly TMPMSDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IInventoryService _inventoryService;

        // Placeholder ảnh mặc định — hình ảnh dược phẩm/thảo dược
        private const string DefaultImageUrl =
            "https://images.unsplash.com/photo-1559056199-641a0ac8b55e?w=400&h=400&fit=crop";

        public ProductImportController(
            TMPMSDbContext db,
            IMemoryCache cache,
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory,
            IInventoryService inventoryService)
        {
            _db = db;
            _cache = cache;
            _env = env;
            _httpClientFactory = httpClientFactory;
            _inventoryService = inventoryService;
        }

        // ================================================================
        // GET /api/admin/products/import/template
        // Trả về file .xlsx mẫu với header + sheet hướng dẫn
        // ================================================================
        [HttpGet("import/template")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadTemplate()
        {
            var workbook = await BuildTemplateWorkbook(null);
            using var ms = new MemoryStream();
            workbook.Write(ms, false);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "mau_nhap_duoc_pham.xlsx");
        }

        // ================================================================
        // GET /api/admin/products/export
        // Xuất TOÀN BỘ danh mục ra Excel kèm ảnh nhúng
        // ================================================================
        [HttpGet("export")]
        [AllowAnonymous]
        public async Task<IActionResult> ExportAll()

        {
            var medicines = await _db.Medicines
                .Include(m => m.Category)
                .Include(m => m.Supplier)
                .OrderBy(m => m.Id)
                .ToListAsync();

            var workbook = await BuildTemplateWorkbook(medicines);

            using var ms = new MemoryStream();
            workbook.Write(ms, false);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"danh_muc_duoc_pham_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // ================================================================
        // POST /api/admin/products/import/preview
        // ================================================================
        [HttpPost("import/preview")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> PreviewImport([FromForm] ImportPreviewRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new { error = "Vui lòng chọn file Excel (.xlsx)" });

            if (!request.File.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Chỉ hỗ trợ file định dạng .xlsx" });

            var categories = await _db.Categories.ToListAsync();
            var suppliers = await _db.Suppliers.ToListAsync();
            var existingMedicines = await _db.Medicines
                .Select(m => new { m.Id, m.Name })
                .ToListAsync();

            using var stream = request.File.OpenReadStream();
            XSSFWorkbook workbook;
            try { workbook = new XSSFWorkbook(stream); }
            catch { return BadRequest(new { error = "File Excel không đọc được, vui lòng kiểm tra lại." }); }

            // Tìm sheet dữ liệu (sheet đầu tiên không phải "Đọc trước")
            ISheet? sheet = null;
            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                var s = workbook.GetSheetAt(i);
                if (!s.SheetName.Contains("Đọc trước") && !s.SheetName.Contains("doc truoc"))
                {
                    sheet = s;
                    break;
                }
            }
            sheet ??= workbook.GetSheetAt(0);

            // ---- Đọc ảnh nhúng, map theo row anchor ----
            var imageByRow = new Dictionary<int, byte[]>();
            var drawing = sheet.DrawingPatriarch as XSSFDrawing;
            if (drawing != null)
            {
                foreach (var shape in drawing.GetShapes())
                {
                    if (shape is XSSFPicture pic)
                    {
                        var anchor = pic.ClientAnchor;
                        var rowIdx = anchor?.Row1 ?? -1;
                        if (rowIdx >= 0 && !imageByRow.ContainsKey(rowIdx))
                            imageByRow[rowIdx] = pic.PictureData.Data;
                    }
                }
            }

            // ---- Parse các dòng dữ liệu ----
            // Header: STT(0) | Tên(1) | Danh mục(2) | Nhà CC(3) | Giá(4) | Giá cũ(5) |
            //         Số lượng(6) | Đơn vị(7) | Mô tả(8) | Hình ảnh(9) | Đánh dấu Xóa(10) | ProductId(11)
            var rows = new List<ImportRowPreview>();

            int dataStartRow = FindDataStartRow(sheet);

            for (int r = dataStartRow; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;

                string CellStr(int col) =>
                    row.GetCell(col)?.ToString()?.Trim() ?? "";

                var name = CellStr(1);
                var categoryName = CellStr(2);
                var supplierName = CellStr(3);
                var priceStr = CellStr(4);
                var oldPriceStr = CellStr(5);
                var stockStr = CellStr(6);
                var unit = CellStr(7);
                var desc = CellStr(8);
                var imageCell = CellStr(9);
                var deleteFlag = CellStr(10);
                var productIdStr = CellStr(11);
                var rxStr = CellStr(12);
                var batchNumberCell = CellStr(13);
                var mfgDateStr = CellStr(14);
                var expDateStr = CellStr(15);
                var costPriceStr = CellStr(16);
                var regNumberCell = CellStr(17);
                var storageConditionCell = CellStr(18);

                // Bỏ qua hàng hoàn toàn trống
                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(priceStr)
                    && string.IsNullOrWhiteSpace(productIdStr)) continue;

                int.TryParse(productIdStr, out int productId);

                var preview = new ImportRowPreview
                {
                    RowIndex = r,
                    Name = name,
                    CategoryName = categoryName,
                    SupplierName = supplierName,
                    PriceStr = priceStr,
                    OldPriceStr = oldPriceStr,
                    StockStr = stockStr,
                    Unit = unit,
                    Description = desc,
                    ProductId = productId,
                    RxCell = rxStr,
                    RequiresPrescription = ParseRxFlag(rxStr),
                    BatchNumber = batchNumberCell,
                    ManufactureDate = ParseExcelDate(row, 14, mfgDateStr),
                    ExpiryDate = ParseExcelDate(row, 15, expDateStr),
                    CostPriceStr = costPriceStr,
                    RegistrationNumber = regNumberCell,
                    StorageCondition = storageConditionCell,
                    Status = "New"
                };

                var errors = new List<string>();

                // ---- Kiểm tra Đánh dấu Xóa ----
                bool isDeleteMark = !string.IsNullOrWhiteSpace(deleteFlag)
                    && (deleteFlag.ToLower().StartsWith("x")
                        || deleteFlag.Contains("có")
                        || deleteFlag.Contains("co")
                        || deleteFlag == "1");

                if (isDeleteMark)
                {
                    preview.Status = "Delete";
                    if (productId <= 0)
                    {
                        preview.Status = "Error";
                        errors.Add("Đánh dấu Xóa nhưng ProductId không hợp lệ — không thể xóa");
                    }
                    else
                    {
                        var exists = existingMedicines.Any(m => m.Id == productId);
                        if (!exists)
                        {
                            preview.Status = "Error";
                            errors.Add($"Không tìm thấy sản phẩm với ProductId={productId}");
                        }
                        else
                        {
                            preview.ExistingId = productId;
                        }
                    }
                }
                else
                {
                    // ---- Validate thông thường ----
                    if (string.IsNullOrWhiteSpace(name))
                        errors.Add("Tên sản phẩm không được rỗng");

                    decimal price = 0;
                    if (!string.IsNullOrWhiteSpace(priceStr))
                    {
                        var cleanPrice = priceStr.Replace(".", "").Replace(",", "");
                        if (!decimal.TryParse(cleanPrice, out price) || price < 0)
                            errors.Add($"Giá bán lẻ không hợp lệ: '{priceStr}'");
                    }
                    preview.Price = price;

                    decimal costPrice = 0;
                    bool costPriceProvided = !string.IsNullOrWhiteSpace(costPriceStr);
                    if (costPriceProvided)
                    {
                        var cleanCost = costPriceStr.Replace(".", "").Replace(",", "");
                        if (!decimal.TryParse(cleanCost, out costPrice) || costPrice < 0)
                        {
                            errors.Add($"Giá nhập không hợp lệ: '{costPriceStr}'");
                            costPriceProvided = false;
                        }
                    }
                    preview.CostPrice = costPrice;

                    // Category
                    var matchedCat = categories.FirstOrDefault(c =>
                        c.Name.Trim().ToLower() == categoryName.Trim().ToLower());
                    if (matchedCat == null && !string.IsNullOrWhiteSpace(categoryName))
                        errors.Add($"Danh mục không tồn tại: '{categoryName}'");
                    preview.CategoryId = matchedCat?.Id ?? 0;

                    // Supplier
                    var matchedSup = suppliers.FirstOrDefault(s =>
                        s.CompanyName.Trim().ToLower() == supplierName.Trim().ToLower());
                    if (matchedSup == null && !string.IsNullOrWhiteSpace(supplierName))
                        errors.Add($"Nhà cung cấp không tồn tại: '{supplierName}'");
                    preview.SupplierId = matchedSup?.Id ?? 0;

                    // ---- Số lượng tồn kho luôn tạo LÔ HÀNG MỚI (có hạn dùng riêng), không cộng dồn mất hạn dùng cũ ----
                    int.TryParse(stockStr.Replace(".", "").Replace(",", ""), out int stockQtyCheck);
                    if (stockQtyCheck > 0)
                    {
                        if (preview.ExpiryDate == null)
                        {
                            preview.Warnings.Add("Có Số lượng tồn kho nhưng thiếu Hạn sử dụng — số lượng sẽ KHÔNG được nhập kho (chỉ cập nhật thông tin khác). Điền cột Hạn sử dụng hoặc dùng tab Kho Dược liệu để nhập lô.");
                        }
                        else if (preview.ExpiryDate.Value.Date < DateTime.Now.Date)
                        {
                            errors.Add("Hạn sử dụng đã ở quá khứ — không thể nhập lô hàng đã hết hạn");
                        }
                        else if (preview.ManufactureDate != null && preview.ExpiryDate.Value.Date <= preview.ManufactureDate.Value.Date)
                        {
                            errors.Add("Hạn sử dụng phải sau Ngày sản xuất");
                        }

                        // Giá nhập BẮT BUỘC khi có Số lượng tồn kho — chặn cứng (không chỉ cảnh báo như HSD)
                        // vì lô thiếu giá vốn sẽ không thể tính lợi nhuận và sẽ bị báo cáo lãi gộp bỏ qua hoàn toàn.
                        if (!costPriceProvided)
                        {
                            errors.Add("Có Số lượng tồn kho nhưng thiếu (hoặc sai định dạng) Giá nhập — bắt buộc phải điền để tính đúng giá vốn/lợi nhuận theo lô");
                        }
                        else if (price > 0 && costPrice > price)
                        {
                            preview.Warnings.Add($"Giá nhập ({costPrice:N0}đ) cao hơn Giá bán lẻ ({price:N0}đ) — lô này sẽ có biên lợi nhuận âm");
                        }
                    }

                    // New vs Update (theo ProductId hoặc tên)
                    if (productId > 0)
                    {
                        var existing = existingMedicines.FirstOrDefault(m => m.Id == productId);
                        if (existing != null)
                        {
                            preview.Status = "Update";
                            preview.ExistingId = productId;
                        }
                        else
                        {
                            errors.Add($"ProductId={productId} không tồn tại trong DB");
                        }
                    }
                    else
                    {
                        var existByName = existingMedicines.FirstOrDefault(m =>
                            m.Name.Trim().ToLower() == name.Trim().ToLower());
                        if (existByName != null)
                        {
                            preview.Status = "Update";
                            preview.ExistingId = existByName.Id;
                        }
                    }

                    if (errors.Count > 0) preview.Status = "Error";
                }

                preview.ErrorMessage = errors.Count > 0 ? string.Join("; ", errors) : null;

                // ---- Xử lý ảnh (ưu tiên: nhúng > URL > mặc định) ----
                if (imageByRow.ContainsKey(r))
                {
                    // Cách A: ảnh nhúng
                    preview.HasImage = true;
                    preview.ImageBytesForCache = imageByRow[r];
                    preview.ImageSourceType = "embedded";
                }
                else if (!string.IsNullOrWhiteSpace(imageCell)
                    && (imageCell.StartsWith("http://") || imageCell.StartsWith("https://")))
                {
                    // Cách B: link URL — KHÔNG tải ảnh ở bước xem trước.
                    // Trước đây tải tuần tự từng URL với timeout 5s → file có hàng trăm URL
                    // không truy cập được (vd: http://image.local/...) bị kẹt "Đang xử lý..." hàng chục phút.
                    // Ảnh URL được tải SONG SONG (có giới hạn thời gian) khi bấm Xác nhận nhập.
                    // Lưu ý: chưa set HasImage=true — chỉ set khi tải URL THÀNH CÔNG,
                    // để URL hỏng không ghi đè ảnh đang dùng / không gán ảnh vỡ cho sản phẩm mới.
                    preview.ImageSourceType = "url";
                    preview.SourceImageUrl = imageCell;
                    preview.Warnings.Add("Ảnh từ link sẽ được tải về khi bạn bấm Xác nhận nhập");
                }
                else if (!string.IsNullOrWhiteSpace(imageCell)
                    && !imageCell.StartsWith("http"))
                {
                    // Text nhưng không phải URL
                    preview.Warnings.Add("Giá trị cột Hình ảnh không hợp lệ (không phải URL hoặc ảnh nhúng)");
                }
                else if (!isDeleteMark)
                {
                    preview.Warnings.Add("Chưa có ảnh — sẽ dùng ảnh mặc định dược phẩm");
                }

                // Tạo thumbnail 150px để hiển thị preview
                if (preview.ImageBytesForCache != null)
                {
                    try
                    {
                        using var imgStream = new MemoryStream(preview.ImageBytesForCache);
                        using var img = await SixLabors.ImageSharp.Image.LoadAsync(imgStream);
                        img.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new SixLabors.ImageSharp.Size(150, 150),
                            Mode = ResizeMode.Max
                        }));
                        using var outStream = new MemoryStream();
                        await img.SaveAsync(outStream, new JpegEncoder { Quality = 75 });
                        preview.ImageThumbnailBase64 = Convert.ToBase64String(outStream.ToArray());
                    }
                    catch
                    {
                        preview.HasImage = false;
                        preview.ImageBytesForCache = null;
                        preview.Warnings.Add("Ảnh không đọc được (định dạng lỗi)");
                    }
                }

                rows.Add(preview);
            }

            var sessionId = Guid.NewGuid().ToString();
            _cache.Set($"import_{sessionId}", rows, TimeSpan.FromMinutes(20));

            return Ok(new
            {
                importSessionId = sessionId,
                totalRows = rows.Count,
                rows = rows.Select(p => new
                {
                    p.RowIndex,
                    p.Name,
                    p.CategoryName,
                    p.SupplierName,
                    price = p.Price,
                    p.StockStr,
                    p.Unit,
                    p.Status,
                    p.ErrorMessage,
                    warnings = p.Warnings,
                    p.HasImage,
                    p.ImageSourceType,
                    imageThumbnailBase64 = p.ImageThumbnailBase64 != null
                        ? $"data:image/jpeg;base64,{p.ImageThumbnailBase64}"
                        : null,
                    p.ProductId,
                    p.ExistingId,
                    requiresPrescription = p.RequiresPrescription,
                    p.BatchNumber,
                    manufactureDate = p.ManufactureDate,
                    expiryDate = p.ExpiryDate,
                    costPrice = p.CostPrice,
                    registrationNumber = p.RegistrationNumber,
                    storageCondition = p.StorageCondition
                })
            });
        }

        // ================================================================
        // POST /api/admin/products/import/confirm
        // ================================================================
        [HttpPost("import/confirm")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> ConfirmImport([FromBody] ImportConfirmRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.ImportSessionId))
                return BadRequest(new { error = "Thiếu importSessionId" });

            if (!_cache.TryGetValue($"import_{req.ImportSessionId}", out List<ImportRowPreview>? cachedRows) || cachedRows == null)
                return BadRequest(new { error = "Phiên import đã hết hạn hoặc không tồn tại. Vui lòng upload lại file." });

            var uploadsDir = Path.Combine(
                _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                "uploads", "medicines");
            Directory.CreateDirectory(uploadsDir);

            var defaultWarehouse = await _db.Warehouses.OrderBy(w => w.Id).FirstOrDefaultAsync();
            int warehouseId = defaultWarehouse?.Id ?? 1;

            int successCount = 0;
            int deletedCount = 0;
            int batchesCreatedCount = 0;
            decimal totalImportValue = 0; // Σ SL × Giá nhập của các lô THỰC SỰ được tạo trong lần chạy này — số thật, không ước tính
            var failedRows = new List<object>();
            var confirmedSet = new HashSet<int>(req.ConfirmedRowIndexes ?? new List<int>());

            // ---- Tải ảnh URL về SONG SONG (giới hạn độ đồng thời & timeout) ----
            // Tránh kẹt khi file có hàng trăm URL không truy cập được (vd: http://image.local/...).
            // Chỉ khi tải URL THÀNH CÔNG mới gắn ảnh (HasImage=true); URL hỏng sẽ bỏ qua
            // → không ghi đè ảnh cũ / không gán ảnh vỡ (giữ nguyên hành vi trước đây).
            var urlRows = cachedRows
                .Where(r => r.ImageSourceType == "url"
                    && r.ImageBytesForCache == null && !string.IsNullOrWhiteSpace(r.SourceImageUrl))
                .ToList();
            if (urlRows.Count > 0)
            {
                using var dlClient = _httpClientFactory.CreateClient();
                dlClient.Timeout = TimeSpan.FromSeconds(5);
                using var sem = new System.Threading.SemaphoreSlim(24);
                var dlTasks = urlRows.Select(async row =>
                {
                    await sem.WaitAsync();
                    try
                    {
                        if (SsrfGuard.IsUnsafeFetchTarget(row.SourceImageUrl!)) return;

                        var bytes = await dlClient.GetByteArrayAsync(row.SourceImageUrl!);
                        if (bytes != null && bytes.Length > 0 && bytes.Length <= 5 * 1024 * 1024
                            && LooksLikeImage(bytes))
                        {
                            row.ImageBytesForCache = bytes;
                            row.HasImage = true;
                        }
                    }
                    catch { /* không tải được → bỏ qua, không gắn ảnh */ }
                    finally { sem.Release(); }
                });
                await Task.WhenAll(dlTasks);
            }

            foreach (var row in cachedRows)
            {
                if (!confirmedSet.Contains(row.RowIndex)) continue;
                if (row.Status == "Error") continue;

                try
                {
                    // ---- DELETE ----
                    if (row.Status == "Delete" && row.ExistingId > 0)
                    {
                        var med = await _db.Medicines.FindAsync(row.ExistingId);
                        if (med == null) continue;

                        // Kiểm tra xem có liên kết với đơn hàng/kê đơn chưa hoàn tất không
                        bool hasActiveLinks =
                            await _db.OrderItems.AnyAsync(oi => oi.MedicineId == row.ExistingId) ||
                            await _db.CartItems.AnyAsync(ci => ci.MedicineId == row.ExistingId) ||
                            await _db.PrescriptionItems.AnyAsync(pi => pi.MedicineId == row.ExistingId) ||
                            await _db.StockBatches.AnyAsync(b => b.MedicineId == row.ExistingId);

                        if (hasActiveLinks)
                        {
                            // Soft delete — ẩn khỏi cửa hàng, giữ lịch sử
                            med.IsActive = false;
                            await _db.SaveChangesAsync();
                        }
                        else
                        {
                            // Hard delete an toàn
                            _db.Medicines.Remove(med);
                            await _db.SaveChangesAsync();
                        }
                        deletedCount++;
                        continue;
                    }

                    // ---- Lưu ảnh ----
                    // Chỉ lưu ảnh khi có bytes thật (ảnh nhúng, hoặc URL tải thành công ở bước trên).
                    // URL hỏng / không có ảnh → dùng ảnh mặc định, KHÔNG gán URL vỡ cho sản phẩm.
                    string imageUrl = DefaultImageUrl;
                    if (row.HasImage && row.ImageBytesForCache != null)
                    {
                        var ext = DetectImageExtension(row.ImageBytesForCache);
                        var fileName = $"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(uploadsDir, fileName);
                        await System.IO.File.WriteAllBytesAsync(filePath, row.ImageBytesForCache);
                        imageUrl = $"/uploads/medicines/{fileName}";
                    }

                    int.TryParse(row.StockStr?.Replace(".", "").Replace(",", ""), out int stockQty);

                    // ---- NEW ----
                    if (row.Status == "New")
                    {
                        var medicine = new Medicine
                        {
                            Name = row.Name,
                            CategoryId = row.CategoryId > 0 ? row.CategoryId : 1,
                            SupplierId = row.SupplierId > 0 ? row.SupplierId : 1,
                            Price = row.Price > 0 ? row.Price : null,
                            OldPrice = decimal.TryParse(row.OldPriceStr?.Replace(".", "").Replace(",", ""), out var op) ? op : null,
                            // Tồn kho chỉ được cộng qua nhập lô (StockBatch) bên dưới — không set thẳng ở đây
                            StockQuantity = 0,
                            Unit = row.Unit,
                            Description = row.Description,
                            ImageUrl = imageUrl,
                            RequiresPrescription = row.RequiresPrescription,
                            IsActive = true,
                            ManufactureDate = DateTime.UtcNow,
                            ExpiryDate = DateTime.UtcNow.AddYears(2),
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.Medicines.Add(medicine);
                        await _db.SaveChangesAsync();

                        // Có tồn kho + có Hạn dùng hợp lệ → tạo LÔ HÀNG riêng (không cộng dồn mất hạn dùng)
                        if (stockQty > 0 && row.ExpiryDate.HasValue)
                        {
                            await _inventoryService.CreateBatch(new StockBatchCreateDTO
                            {
                                MedicineId = medicine.Id,
                                WarehouseId = warehouseId,
                                BatchNumber = string.IsNullOrWhiteSpace(row.BatchNumber) ? null : row.BatchNumber,
                                ManufactureDate = row.ManufactureDate ?? DateTime.Now,
                                ExpiryDate = row.ExpiryDate.Value,
                                Quantity = stockQty,
                                UnitCostPrice = row.CostPrice > 0 ? row.CostPrice : (decimal?)null,
                                SupplierId = row.SupplierId > 0 ? row.SupplierId : (int?)null,
                                RegistrationNumber = string.IsNullOrWhiteSpace(row.RegistrationNumber) ? null : row.RegistrationNumber,
                                StorageCondition = string.IsNullOrWhiteSpace(row.StorageCondition) ? null : row.StorageCondition,
                                Note = $"Nhập qua Excel — phiên {req.ImportSessionId[..8]}"
                            });
                            batchesCreatedCount++;
                            totalImportValue += stockQty * row.CostPrice;
                        }
                        successCount++;
                    }
                    // ---- UPDATE ----
                    else if (row.Status == "Update" && row.ExistingId > 0)
                    {
                        var med = await _db.Medicines.FindAsync(row.ExistingId);
                        if (med != null)
                        {
                            if (!string.IsNullOrWhiteSpace(row.Name)) med.Name = row.Name;
                            if (row.Price > 0) med.Price = row.Price;
                            if (!string.IsNullOrWhiteSpace(row.Unit)) med.Unit = row.Unit;
                            if (!string.IsNullOrWhiteSpace(row.Description)) med.Description = row.Description;
                            if (row.HasImage) med.ImageUrl = imageUrl;
                            if (row.CategoryId > 0) med.CategoryId = row.CategoryId;
                            if (row.SupplierId > 0) med.SupplierId = row.SupplierId;
                            // Chỉ cập nhật cờ kê đơn khi cột Excel có giá trị — tránh xoá cờ true của dữ liệu cũ khi import file template cũ (cột trống)
                            if (!string.IsNullOrWhiteSpace(row.RxCell)) med.RequiresPrescription = row.RequiresPrescription;
                            med.IsActive = true; // đảm bảo không bị ẩn
                            await _db.SaveChangesAsync();

                            // Có tồn kho + có Hạn dùng hợp lệ → tạo LÔ HÀNG riêng (không cộng dồn mất hạn dùng cũ)
                            if (stockQty > 0 && row.ExpiryDate.HasValue)
                            {
                                await _inventoryService.CreateBatch(new StockBatchCreateDTO
                                {
                                    MedicineId = med.Id,
                                    WarehouseId = warehouseId,
                                    BatchNumber = string.IsNullOrWhiteSpace(row.BatchNumber) ? null : row.BatchNumber,
                                    ManufactureDate = row.ManufactureDate ?? DateTime.Now,
                                    ExpiryDate = row.ExpiryDate.Value,
                                    Quantity = stockQty,
                                    UnitCostPrice = row.CostPrice > 0 ? row.CostPrice : (decimal?)null,
                                    SupplierId = row.SupplierId > 0 ? row.SupplierId : (int?)null,
                                    RegistrationNumber = string.IsNullOrWhiteSpace(row.RegistrationNumber) ? null : row.RegistrationNumber,
                                    StorageCondition = string.IsNullOrWhiteSpace(row.StorageCondition) ? null : row.StorageCondition,
                                    Note = $"Nhập qua Excel — phiên {req.ImportSessionId[..8]}"
                                });
                                batchesCreatedCount++;
                                totalImportValue += stockQty * row.CostPrice;
                            }
                            successCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    failedRows.Add(new { row.RowIndex, row.Name, error = ex.Message });
                }
            }

            _cache.Remove($"import_{req.ImportSessionId}");

            return Ok(new
            {
                successCount,
                deletedCount,
                failedCount = failedRows.Count,
                failedRows,
                batchesCreatedCount,
                totalImportValue
            });
        }

        // ================================================================
        // HELPERS
        // ================================================================

        /// <summary>Xây dựng workbook Excel — nếu medicines != null thì ghi dữ liệu, ngược lại chỉ trả template rỗng</summary>
        private async Task<XSSFWorkbook> BuildTemplateWorkbook(List<Medicine>? medicines)
        {
            var workbook = new XSSFWorkbook();

            // ---- Sheet 1: Hướng dẫn ----
            var guide = workbook.CreateSheet("Đọc trước");
            var guideRows = new[]
            {
                "📌 HƯỚNG DẪN NHẬP LIỆU DƯỢC PHẨM",
                "",
                "Cột Hình ảnh — hỗ trợ CẢ 2 cách, hệ thống tự nhận diện:",
                "  • CÁCH A: Dán ảnh trực tiếp (Ctrl+V) vào ô cột Hình ảnh — ảnh sẽ được nhúng vào file",
                "  • CÁCH B: Gõ/dán ĐƯỜNG LINK ẢNH (https://...) trực tiếp vào ô — hệ thống sẽ tải ảnh về tự động",
                "  → Không cần làm gì thêm, hệ thống tự nhận diện cả 2 cách",
                "",
                "Cột Đánh dấu Xóa — điền 'X' vào ô này để xóa sản phẩm (cần có ProductId đúng ở cột 12)",
                "  ⚠️ Sản phẩm đã có đơn hàng lịch sử sẽ được ẩn khỏi cửa hàng (không hard-delete)",
                "",
                "Cột ProductId (cột xám cuối) — KHÔNG XÓA CỘT NÀY",
                "  • Hệ thống dùng cột này để đối chiếu Update/Delete — xóa đi sẽ mất khả năng cập nhật chính xác",
                "",
                "Cột Số lượng tồn kho — MỖI LẦN NHẬP TẠO 1 LÔ HÀNG RIÊNG, không cộng dồn mất hạn dùng cũ:",
                "  • Nếu điền Số lượng tồn kho > 0, BẮT BUỘC phải điền cột Hạn sử dụng (dd/mm/yyyy) VÀ Giá nhập",
                "  • Thiếu Hạn sử dụng → số lượng sẽ KHÔNG được nhập kho (chỉ cập nhật các thông tin khác)",
                "  • Thiếu Giá nhập → dòng bị LỖI, KHÔNG nhập được (bắt buộc để tính đúng lợi nhuận theo lô)",
                "  • Cột Số lô để trống → hệ thống tự sinh số lô",
                "  • Cột Ngày sản xuất để trống → mặc định là ngày nhập",
                "  • Cột SĐK / Điều kiện bảo quản: không bắt buộc, chỉ để ghi chú vào lô",
                "  • Nếu số lô trùng với lô đang có (cùng NSX/HSD), giá nhập sẽ được tính BÌNH QUÂN GIA QUYỀN",
                "    theo số lượng còn lại hiện có, không ghi đè giá cũ",
                "",
                "Các trường bắt buộc: Tên sản phẩm, Danh mục, Giá bán lẻ",
                "Danh mục phải khớp chính xác tên danh mục đã có trong hệ thống",
            };
            for (int i = 0; i < guideRows.Length; i++)
            {
                var gr = guide.CreateRow(i);
                gr.CreateCell(0).SetCellValue(guideRows[i]);
            }
            guide.SetColumnWidth(0, 25000);

            // ---- Sheet 2: Dữ liệu ----
            var sheet = workbook.CreateSheet("Dược Phẩm");

            // Styles
            var headerStyle = CreateStyle(workbook, bold: true, bgColor: NPOI.HSSF.Util.HSSFColor.LightGreen.Index);
            var idColStyle = CreateStyle(workbook, bold: false, bgColor: NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index);
            var deleteColStyle = CreateStyle(workbook, bold: true, bgColor: NPOI.HSSF.Util.HSSFColor.Rose.Index);
            var noteStyle = CreateStyle(workbook, bold: false, italic: true,
                fontColor: NPOI.HSSF.Util.HSSFColor.DarkRed.Index);

            // Column widths
            int[] colWidths = { 2000, 8000, 6000, 7000, 4000, 4000, 4000, 3000, 10000, 8000, 4000, 3000, 4500, 4500, 4500, 4500, 4500, 5000, 6000 };
            for (int i = 0; i < colWidths.Length; i++)
                sheet.SetColumnWidth(i, colWidths[i]);

            // Header row 0
            string[] headers = {
                "STT", "Tên sản phẩm", "Danh mục", "Nhà cung cấp",
                "Giá bán lẻ", "Giá niêm yết cũ", "Số lượng tồn kho",
                "Đơn vị", "Mô tả", "Hình ảnh", "Đánh dấu Xóa", "ProductId", "Yêu cầu kê đơn (Có/Không)",
                "Số lô", "Ngày sản xuất", "Hạn sử dụng",
                "Giá nhập", "Số đăng ký (SĐK)/GP", "Điều kiện bảo quản"
            };
            var headerRow = sheet.CreateRow(0);
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = (i == 10) ? deleteColStyle : (i == 11) ? idColStyle : headerStyle;
            }

            // Ghi chú row 1
            var noteRow = sheet.CreateRow(1);
            noteRow.CreateCell(9).SetCellValue("← Dán ảnh (Ctrl+V) hoặc link https://... — tự nhận diện cả 2");
            noteRow.GetCell(9).CellStyle = noteStyle;
            noteRow.CreateCell(10).SetCellValue("X = xóa");
            noteRow.GetCell(10).CellStyle = noteStyle;
            noteRow.CreateCell(11).SetCellValue("← KHÔNG XÓA CỘT NÀY");
            noteRow.GetCell(11).CellStyle = noteStyle;
            noteRow.CreateCell(12).SetCellValue("Có = cần kê đơn, để trống = không");
            noteRow.GetCell(12).CellStyle = noteStyle;
            noteRow.CreateCell(13).SetCellValue("Để trống sẽ tự sinh");
            noteRow.GetCell(13).CellStyle = noteStyle;
            noteRow.CreateCell(14).SetCellValue("dd/mm/yyyy, để trống = ngày nhập");
            noteRow.GetCell(14).CellStyle = noteStyle;
            noteRow.CreateCell(15).SetCellValue("dd/mm/yyyy — BẮT BUỘC nếu có Số lượng tồn kho");
            noteRow.GetCell(15).CellStyle = noteStyle;
            noteRow.CreateCell(16).SetCellValue("BẮT BUỘC nếu có Số lượng tồn kho — dùng tính lợi nhuận");
            noteRow.GetCell(16).CellStyle = noteStyle;
            noteRow.CreateCell(17).SetCellValue("Không bắt buộc");
            noteRow.GetCell(17).CellStyle = noteStyle;
            noteRow.CreateCell(18).SetCellValue("Không bắt buộc — vd: Kho Thường / Kho Mát / Cold Chain");
            noteRow.GetCell(18).CellStyle = noteStyle;

            if (medicines == null || medicines.Count == 0)
            {
                // Template rỗng — dòng ví dụ row 2
                var exRow = sheet.CreateRow(2);
                string[] example = {
                    "1", "Bạch truật thảo dược", "Thảo dược & Đông Y", "Dược liệu Việt Nam",
                    "85000", "100000", "200", "Túi 100g",
                    "Bạch truật khô hỗ trợ tiêu hoá, bổ tỳ vị", "", "", "", "Không",
                    "", DateTime.Now.ToString("dd/MM/yyyy"), DateTime.Now.AddYears(1).ToString("dd/MM/yyyy"),
                    "60000", "", "Kho Thường (<30°C)"
                };
                for (int i = 0; i < example.Length; i++)
                    exRow.CreateCell(i).SetCellValue(example[i]);
                exRow.HeightInPoints = 80;
            }
            else
            {
                // Export thật — ghi dữ liệu từ DB kèm ảnh nhúng
                var drawing = sheet.CreateDrawingPatriarch() as XSSFDrawing;
                var uploadsDir = Path.Combine(
                    _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                    "uploads", "medicines");

                for (int i = 0; i < medicines.Count; i++)
                {
                    var med = medicines[i];
                    int rowIdx = i + 2; // skip header (0) và note (1)

                    var row = sheet.CreateRow(rowIdx);
                    row.HeightInPoints = 72;

                    row.CreateCell(0).SetCellValue(i + 1);
                    row.CreateCell(1).SetCellValue(med.Name ?? "");
                    row.CreateCell(2).SetCellValue(med.Category?.Name ?? "");
                    row.CreateCell(3).SetCellValue(med.Supplier?.CompanyName ?? "");
                    row.CreateCell(4).SetCellValue((double)(med.Price ?? 0));
                    row.CreateCell(5).SetCellValue((double)(med.OldPrice ?? 0));
                    // Để trống Số lượng tồn kho khi xuất — tồn kho quản lý theo lô ở tab Kho Dược liệu,
                    // xuất ra rồi nhập lại nguyên file sẽ không cộng/xóa nhầm tồn kho hiện có.
                    // Chỉ điền cột này (kèm Hạn sử dụng) khi THỰC SỰ muốn nhập thêm kho qua Excel.
                    row.CreateCell(7).SetCellValue(med.Unit ?? "");
                    row.CreateCell(8).SetCellValue(med.Description ?? "");

                    // Cột Hình ảnh: nhúng ảnh nếu có, hoặc ghi URL
                    bool imageEmbedded = false;
                    if (!string.IsNullOrWhiteSpace(med.ImageUrl))
                    {
                        byte[]? imgBytes = null;

                        if (med.ImageUrl.StartsWith("/uploads/"))
                        {
                            // Ảnh local
                            var localPath = Path.Combine(
                                _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                                med.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                            if (System.IO.File.Exists(localPath))
                                imgBytes = await System.IO.File.ReadAllBytesAsync(localPath);
                        }
                        else if (med.ImageUrl.StartsWith("http"))
                        {
                            // Tải ảnh từ URL ngoài — dùng timeout 3s để không chặn quá lâu
                            try
                            {
                                if (!SsrfGuard.IsUnsafeFetchTarget(med.ImageUrl))
                                {
                                    var client = _httpClientFactory.CreateClient();
                                    client.Timeout = TimeSpan.FromSeconds(3);
                                    imgBytes = await client.GetByteArrayAsync(med.ImageUrl);
                                }
                            }
                            catch { /* bỏ qua, ghi URL */ }
                        }

                        if (imgBytes != null && imgBytes.Length > 0 && imgBytes.Length <= 3 * 1024 * 1024)
                        {
                            try
                            {
                                // Resize về 200px để giảm kích thước file
                                using var imgStream = new MemoryStream(imgBytes);
                                using var img = await SixLabors.ImageSharp.Image.LoadAsync(imgStream);
                                img.Mutate(x => x.Resize(new ResizeOptions
                                {
                                    Size = new SixLabors.ImageSharp.Size(200, 200),
                                    Mode = ResizeMode.Max
                                }));
                                using var outMs = new MemoryStream();
                                await img.SaveAsync(outMs, new JpegEncoder { Quality = 80 });
                                imgBytes = outMs.ToArray();

                                int picIdx = workbook.AddPicture(imgBytes, PictureType.JPEG);
                                var anchor = new XSSFClientAnchor(0, 0, 0, 0, 9, rowIdx, 10, rowIdx + 1);
                                anchor.AnchorType = AnchorType.MoveAndResize;
                                drawing?.CreatePicture(anchor, picIdx);
                                imageEmbedded = true;
                            }
                            catch { /* ghi URL nếu nhúng lỗi */ }
                        }

                        if (!imageEmbedded)
                            row.CreateCell(9).SetCellValue(med.ImageUrl);
                    }

                    // Cột Đánh dấu Xóa — để trống
                    row.CreateCell(10).SetCellValue("");

                    // Cột ProductId — màu xám
                    var idCell = row.CreateCell(11);
                    idCell.SetCellValue(med.Id);
                    idCell.CellStyle = idColStyle;

                    // Cột Yêu cầu kê đơn (Có/Không)
                    row.CreateCell(12).SetCellValue(med.RequiresPrescription ? "Có" : "Không");
                }
            }

            return workbook;
        }

        /// <summary>Đọc ngày từ ô Excel — ưu tiên ô định dạng ngày thật, sau đó thử parse chuỗi text (dd/MM/yyyy, ...)</summary>
        private static DateTime? ParseExcelDate(IRow row, int col, string cellText)
        {
            var cell = row.GetCell(col);
            if (cell != null && cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                return cell.DateCellValue;

            if (string.IsNullOrWhiteSpace(cellText)) return null;

            var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "MM/dd/yyyy" };
            if (DateTime.TryParseExact(cellText, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
                return exact;
            if (DateTime.TryParse(cellText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var generic))
                return generic;
            return null;
        }

        private static int FindDataStartRow(ISheet sheet)
        {
            // Tìm hàng đầu tiên có dữ liệu thật (bỏ qua header và ghi chú)
            // Header thường ở row 0, ghi chú ở row 1 → dữ liệu từ row 2
            return 2;
        }

        private static ICellStyle CreateStyle(IWorkbook wb, bool bold = false, bool italic = false,
            short bgColor = 0, short fontColor = 0)
        {
            var style = wb.CreateCellStyle();
            var font = wb.CreateFont();
            font.IsBold = bold;
            font.IsItalic = italic;
            if (fontColor != 0) font.Color = fontColor;
            style.SetFont(font);
            if (bgColor != 0)
            {
                style.FillForegroundColor = bgColor;
                style.FillPattern = FillPattern.SolidForeground;
            }
            return style;
        }

        private static bool LooksLikeImage(byte[] data)
        {
            if (data.Length < 4) return false;
            if (data[0] == 0xFF && data[1] == 0xD8) return true;          // JPEG
            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E) return true; // PNG
            if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46) return true; // GIF
            if (data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46) return true; // RIFF (WEBP)
            if (data[0] == 0x42 && data[1] == 0x4D) return true;          // BMP
            return false;
        }

        private static string DetectImageExtension(byte[] data)
        {
            if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8) return ".jpg";
            if (data.Length >= 8 && data[0] == 0x89 && data[1] == 0x50) return ".png";
            if (data.Length >= 3 && data[0] == 0x47 && data[1] == 0x49) return ".gif";
            if (data.Length >= 4 && data[0] == 0x52 && data[1] == 0x49) return ".webp";
            return ".jpg";
        }

        // "Có", "Co", "1", "True", "X", "Y", "Yes", "Cần" -> true; "Không"/trống -> false
        private static bool ParseRxFlag(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var v = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            v = new string(v.Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
            switch (v)
            {
                case "co":
                case "1":
                case "true":
                case "x":
                case "y":
                case "yes":
                case "can":
                case "batbuoc":
                    return true;
                default:
                    return false;
            }
        }
    }

    // ================================================================
    // DTO Models
    // ================================================================
    public class ImportRowPreview
    {
        public int RowIndex { get; set; }
        public string Name { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string SupplierName { get; set; } = "";
        public string PriceStr { get; set; } = "";
        public string OldPriceStr { get; set; } = "";
        public string StockStr { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public int SupplierId { get; set; }
        public int ExistingId { get; set; }
        public int ProductId { get; set; }
        public string RxCell { get; set; } = "";   // giá trị thô cột "Yêu cầu kê đơn"
        public bool RequiresPrescription { get; set; } // đã parse (false nếu cột trống)
        public string BatchNumber { get; set; } = ""; // số lô — để trống sẽ tự sinh
        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string CostPriceStr { get; set; } = "";
        public decimal CostPrice { get; set; }
        public string RegistrationNumber { get; set; } = "";
        public string StorageCondition { get; set; } = "";
        public string Status { get; set; } = "New"; // New | Update | Delete | Error
        public string? ErrorMessage { get; set; }
        public List<string> Warnings { get; set; } = new();
        public bool HasImage { get; set; }
        public string? ImageSourceType { get; set; } // "embedded" | "url"
        public string? SourceImageUrl { get; set; }  // dùng khi type = url
        public string? ImageThumbnailBase64 { get; set; }
        public byte[]? ImageBytesForCache { get; set; } // NOT serialized
    }

    public class ImportConfirmRequest
    {
        public string ImportSessionId { get; set; } = "";
        public List<int> ConfirmedRowIndexes { get; set; } = new();
    }
}
