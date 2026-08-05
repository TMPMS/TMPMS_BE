using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BusinessObjects;
using TMPMS.Data;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/admin/products")]
    public class ProductImportController : ControllerBase
    {
        private readonly TMPMSDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpClientFactory;

        // Placeholder ảnh mặc định — hình ảnh dược phẩm/thảo dược
        private const string DefaultImageUrl =
            "https://images.unsplash.com/photo-1559056199-641a0ac8b55e?w=400&h=400&fit=crop";

        public ProductImportController(
            TMPMSDbContext db,
            IMemoryCache cache,
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _cache = cache;
            _env = env;
            _httpClientFactory = httpClientFactory;
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
        public async Task<IActionResult> PreviewImport([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Vui lòng chọn file Excel (.xlsx)" });

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Chỉ hỗ trợ file định dạng .xlsx" });

            var categories = await _db.Categories.ToListAsync();
            var suppliers = await _db.Suppliers.ToListAsync();
            var existingMedicines = await _db.Medicines
                .Select(m => new { m.Id, m.Name })
                .ToListAsync();

            using var stream = file.OpenReadStream();
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
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

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
                    // Cách B: link URL
                    bool looksLikeImage = imageCell.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                        || imageCell.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                        || imageCell.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                        || imageCell.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
                        || imageCell.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);

                    if (looksLikeImage || true) // luôn thử tải
                    {
                        try
                        {
                            var resp = await httpClient.GetAsync(imageCell, HttpCompletionOption.ResponseHeadersRead);
                            resp.EnsureSuccessStatusCode();
                            var ct = resp.Content.Headers.ContentType?.MediaType ?? "";
                            if (ct.StartsWith("image/"))
                            {
                                var imgBytes = await resp.Content.ReadAsByteArrayAsync();
                                if (imgBytes.Length <= 5 * 1024 * 1024) // max 5MB
                                {
                                    preview.HasImage = true;
                                    preview.ImageBytesForCache = imgBytes;
                                    preview.ImageSourceType = "url";
                                    preview.SourceImageUrl = imageCell;
                                }
                                else
                                {
                                    preview.Warnings.Add("Ảnh từ link quá lớn (>5MB), sẽ dùng ảnh mặc định");
                                }
                            }
                            else
                            {
                                preview.Warnings.Add("Link không trả về ảnh hợp lệ, sẽ dùng ảnh mặc định");
                            }
                        }
                        catch
                        {
                            preview.Warnings.Add("Không tải được ảnh từ link, sẽ dùng ảnh mặc định");
                        }
                    }
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
                    requiresPrescription = p.RequiresPrescription
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
            var failedRows = new List<object>();
            var confirmedSet = new HashSet<int>(req.ConfirmedRowIndexes ?? new List<int>());

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
                            await _db.PrescriptionItems.AnyAsync(pi => pi.MedicineId == row.ExistingId);

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
                    string imageUrl = row.SourceImageUrl ?? DefaultImageUrl;
                    if (row.HasImage && row.ImageBytesForCache != null && row.ImageSourceType == "embedded")
                    {
                        var ext = DetectImageExtension(row.ImageBytesForCache);
                        var fileName = $"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(uploadsDir, fileName);
                        await System.IO.File.WriteAllBytesAsync(filePath, row.ImageBytesForCache);
                        imageUrl = $"/uploads/medicines/{fileName}";
                    }
                    else if (row.HasImage && row.ImageBytesForCache != null && row.ImageSourceType == "url")
                    {
                        // Lưu bản sao local cho URL ảnh ngoài
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
                            StockQuantity = stockQty,
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

                        if (stockQty > 0)
                        {
                            _db.InventoryTransactions.Add(new InventoryTransaction
                            {
                                MedicineId = medicine.Id,
                                WarehouseId = warehouseId,
                                Type = "Import",
                                Quantity = stockQty,
                                ReferenceId = $"BULK_IMPORT_{req.ImportSessionId[..8]}",
                                CreatedAt = DateTime.UtcNow
                            });
                            await _db.SaveChangesAsync();
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

                            if (stockQty > 0)
                            {
                                med.StockQuantity += stockQty;
                                _db.InventoryTransactions.Add(new InventoryTransaction
                                {
                                    MedicineId = med.Id,
                                    WarehouseId = warehouseId,
                                    Type = "Import",
                                    Quantity = stockQty,
                                    ReferenceId = $"BULK_IMPORT_{req.ImportSessionId[..8]}",
                                    CreatedAt = DateTime.UtcNow
                                });
                            }
                            await _db.SaveChangesAsync();
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
                failedRows
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
            int[] colWidths = { 2000, 8000, 6000, 7000, 4000, 4000, 4000, 3000, 10000, 8000, 4000, 3000, 4500 };
            for (int i = 0; i < colWidths.Length; i++)
                sheet.SetColumnWidth(i, colWidths[i]);

            // Header row 0
            string[] headers = {
                "STT", "Tên sản phẩm", "Danh mục", "Nhà cung cấp",
                "Giá bán lẻ", "Giá niêm yết cũ", "Số lượng tồn kho",
                "Đơn vị", "Mô tả", "Hình ảnh", "Đánh dấu Xóa", "ProductId", "Yêu cầu kê đơn (Có/Không)"
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

            if (medicines == null || medicines.Count == 0)
            {
                // Template rỗng — dòng ví dụ row 2
                var exRow = sheet.CreateRow(2);
                string[] example = {
                    "1", "Bạch truật thảo dược", "Thảo dược & Đông Y", "Dược liệu Việt Nam",
                    "85000", "100000", "200", "Túi 100g",
                    "Bạch truật khô hỗ trợ tiêu hoá, bổ tỳ vị", "", "", "", "Không"
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
                    row.CreateCell(6).SetCellValue(med.StockQuantity);
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
                                var client = _httpClientFactory.CreateClient();
                                client.Timeout = TimeSpan.FromSeconds(3);
                                imgBytes = await client.GetByteArrayAsync(med.ImageUrl);
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
