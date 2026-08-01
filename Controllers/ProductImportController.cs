using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObjects;
using TMPMS.Data;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/admin/products")]
    [Authorize]
    public class ProductImportController : ControllerBase
    {
        private readonly TMPMSDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;

        public ProductImportController(TMPMSDbContext db, IMemoryCache cache, IWebHostEnvironment env)
        {
            _db = db;
            _cache = cache;
            _env = env;
        }

        // ================================================================
        // BƯỚC 2 — GET /api/admin/products/import/template
        // Trả về file .xlsx mẫu với header + 1 dòng minh họa
        // ================================================================
        [HttpGet("import/template")]
        public IActionResult DownloadTemplate()
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Dược Phẩm");

            // Style cho header
            var headerStyle = workbook.CreateCellStyle();
            var headerFont = workbook.CreateFont();
            headerFont.IsBold = true;
            headerStyle.SetFont(headerFont);
            headerStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;

            var noteStyle = workbook.CreateCellStyle();
            var noteFont = workbook.CreateFont();
            noteFont.IsItalic = true;
            noteFont.Color = NPOI.HSSF.Util.HSSFColor.DarkRed.Index;
            noteStyle.SetFont(noteFont);

            // Header row 0
            string[] headers = {
                "STT", "Tên sản phẩm", "Danh mục", "Nhà cung cấp",
                "Giá bán lẻ", "Giá niêm yết cũ", "Số lượng tồn kho",
                "Đơn vị", "Mô tả", "Hình ảnh"
            };
            var headerRow = sheet.CreateRow(0);
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
                sheet.SetColumnWidth(i, i == 8 ? 10000 : (i == 1 ? 8000 : 5000)); // wider desc & name cols
            }

            // Ghi chú row 1
            var noteRow = sheet.CreateRow(1);
            var noteCell = noteRow.CreateCell(9);
            noteCell.SetCellValue("← Dán ảnh trực tiếp vào ô cột này, không dán link URL");
            noteCell.CellStyle = noteStyle;

            // Dòng ví dụ mẫu row 2
            var exRow = sheet.CreateRow(2);
            string[] example = {
                "1", "Bạch truật thảo dược", "Thảo dược & Đông Y", "Dược liệu Việt Nam",
                "85000", "100000", "200", "Túi 100g", "Bạch truật khô hỗ trợ tiêu hoá, bổ tỳ vị", ""
            };
            for (int i = 0; i < example.Length; i++)
                exRow.CreateCell(i).SetCellValue(example[i]);

            // Set row height for image rows to 100px ~= 75 points
            for (int r = 2; r <= 20; r++)
            {
                var row = sheet.GetRow(r) ?? sheet.CreateRow(r);
                row.HeightInPoints = 80;
            }

            using var ms = new MemoryStream();
            workbook.Write(ms, false);
            var bytes = ms.ToArray();

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "mau_nhap_duoc_pham.xlsx");
        }

        // ================================================================
        // BƯỚC 3 — POST /api/admin/products/import/preview
        // Đọc xlsx, validate, trả JSON preview + cache
        // ================================================================
        [HttpPost("import/preview")]
        [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB
        public async Task<IActionResult> PreviewImport([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Vui lòng chọn file Excel (.xlsx)" });

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Chỉ hỗ trợ file định dạng .xlsx" });

            // Load danh sách Category và Supplier để so khớp tên
            var categories = await _db.Categories.ToListAsync();
            var suppliers = await _db.Suppliers.ToListAsync();
            var existingMedicines = await _db.Medicines.Select(m => new { m.Id, m.Name }).ToListAsync();

            using var stream = file.OpenReadStream();
            XSSFWorkbook workbook;
            try { workbook = new XSSFWorkbook(stream); }
            catch { return BadRequest(new { error = "File Excel không đọc được, vui lòng kiểm tra lại." }); }

            var sheet = workbook.GetSheetAt(0);

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
                        {
                            imageByRow[rowIdx] = pic.PictureData.Data;
                        }
                    }
                }
            }

            // ---- Parse các dòng dữ liệu (bỏ qua row 0 = header, row 1 = ghi chú) ----
            var rows = new List<ImportRowPreview>();

            for (int r = 2; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;

                string CellStr(int col) =>
                    row.GetCell(col)?.ToString()?.Trim() ?? "";

                var stt = CellStr(0);
                var name = CellStr(1);
                var categoryName = CellStr(2);
                var supplierName = CellStr(3);
                var priceStr = CellStr(4);
                var oldPriceStr = CellStr(5);
                var stockStr = CellStr(6);
                var unit = CellStr(7);
                var desc = CellStr(8);

                // Bỏ qua hàng trống hoàn toàn
                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(priceStr)) continue;

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
                    Status = "New",
                    HasImage = imageByRow.ContainsKey(r)
                };

                // --- Validate ---
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(name))
                    errors.Add("Tên sản phẩm không được rỗng");

                decimal price = 0;
                if (!string.IsNullOrWhiteSpace(priceStr))
                {
                    // loại bỏ dấu chấm/phẩy ngăn cách hàng nghìn
                    var cleanPrice = priceStr.Replace(".", "").Replace(",", "");
                    if (!decimal.TryParse(cleanPrice, out price) || price < 0)
                        errors.Add($"Giá bán lẻ không hợp lệ: '{priceStr}'");
                }
                preview.Price = price;

                // So khớp Category
                var matchedCat = categories.FirstOrDefault(c =>
                    c.Name.Trim().ToLower() == categoryName.Trim().ToLower());
                if (matchedCat == null && !string.IsNullOrWhiteSpace(categoryName))
                    errors.Add($"Danh mục không tồn tại: '{categoryName}'");
                preview.CategoryId = matchedCat?.Id ?? 0;

                // So khớp Supplier
                var matchedSup = suppliers.FirstOrDefault(s =>
                    s.CompanyName.Trim().ToLower() == supplierName.Trim().ToLower());
                if (matchedSup == null && !string.IsNullOrWhiteSpace(supplierName))
                    errors.Add($"Nhà cung cấp không tồn tại: '{supplierName}'");
                preview.SupplierId = matchedSup?.Id ?? 0;

                // Ảnh: Warning nếu thiếu
                if (!preview.HasImage)
                    preview.Warnings.Add("Không có ảnh nhúng — sẽ dùng ảnh mặc định");

                // Trùng tên => Update
                var existing = existingMedicines.FirstOrDefault(m =>
                    m.Name.Trim().ToLower() == name.Trim().ToLower());
                if (existing != null)
                {
                    preview.Status = "Update";
                    preview.ExistingId = existing.Id;
                }

                if (errors.Count > 0)
                {
                    preview.Status = "Error";
                    preview.ErrorMessage = string.Join("; ", errors);
                }

                // Thumbnail: resize ảnh xuống ~150px rồi base64
                if (preview.HasImage)
                {
                    try
                    {
                        var imgBytes = imageByRow[r];
                        using var imgStream = new MemoryStream(imgBytes);
                        using var img = await Image.LoadAsync(imgStream);
                        img.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(150, 150),
                            Mode = ResizeMode.Max
                        }));
                        using var outStream = new MemoryStream();
                        await img.SaveAsync(outStream, new JpegEncoder { Quality = 75 });
                        preview.ImageThumbnailBase64 = Convert.ToBase64String(outStream.ToArray());
                        // Lưu bytes gốc vào cache data (không đưa vào JSON response)
                        preview.ImageBytesForCache = imgBytes;
                    }
                    catch
                    {
                        preview.HasImage = false;
                        preview.Warnings.Add("Ảnh nhúng không đọc được");
                    }
                }

                rows.Add(preview);
            }

            // ---- Cache toàn bộ dữ liệu 20 phút ----
            var sessionId = Guid.NewGuid().ToString();
            _cache.Set($"import_{sessionId}", rows, TimeSpan.FromMinutes(20));

            // ---- Build JSON response (không lộ ImageBytesForCache ra ngoài) ----
            var response = new
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
                    imageThumbnailBase64 = p.ImageThumbnailBase64 != null
                        ? $"data:image/jpeg;base64,{p.ImageThumbnailBase64}"
                        : null
                })
            };

            return Ok(response);
        }

        // ================================================================
        // BƯỚC 4 — POST /api/admin/products/import/confirm
        // Ghi thật vào DB
        // ================================================================
        [HttpPost("import/confirm")]
        public async Task<IActionResult> ConfirmImport([FromBody] ImportConfirmRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.ImportSessionId))
                return BadRequest(new { error = "Thiếu importSessionId" });

            if (!_cache.TryGetValue($"import_{req.ImportSessionId}", out List<ImportRowPreview>? cachedRows) || cachedRows == null)
                return BadRequest(new { error = "Phiên import đã hết hạn hoặc không tồn tại. Vui lòng upload lại file." });

            // Tạo thư mục lưu ảnh nếu chưa có
            var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "medicines");
            Directory.CreateDirectory(uploadsDir);

            // Lấy warehouse mặc định (warehouse đầu tiên)
            var defaultWarehouse = await _db.Warehouses.OrderBy(w => w.Id).FirstOrDefaultAsync();
            int warehouseId = defaultWarehouse?.Id ?? 1;

            int successCount = 0;
            var failedRows = new List<object>();

            var confirmedSet = new HashSet<int>(req.ConfirmedRowIndexes ?? new List<int>());

            foreach (var row in cachedRows)
            {
                if (!confirmedSet.Contains(row.RowIndex)) continue;
                if (row.Status == "Error") continue;

                try
                {
                    // Lưu ảnh vào wwwroot/uploads/medicines/
                    string imageUrl = "https://images.unsplash.com/photo-1515377905703-c4788e51af15?w=400";
                    if (row.HasImage && row.ImageBytesForCache != null)
                    {
                        var ext = DetectImageExtension(row.ImageBytesForCache);
                        var fileName = $"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(uploadsDir, fileName);
                        await System.IO.File.WriteAllBytesAsync(filePath, row.ImageBytesForCache);
                        imageUrl = $"/uploads/medicines/{fileName}";
                    }

                    // Parse stock
                    int.TryParse(row.StockStr?.Replace(".", "").Replace(",", ""), out int stockQty);

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
                            RequiresPrescription = false,
                            ManufactureDate = DateTime.UtcNow,
                            ExpiryDate = DateTime.UtcNow.AddYears(2),
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.Medicines.Add(medicine);
                        await _db.SaveChangesAsync();

                        // Ghi InventoryTransaction "Import"
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

            // Xóa cache sau khi confirm
            _cache.Remove($"import_{req.ImportSessionId}");

            return Ok(new
            {
                successCount,
                failedCount = failedRows.Count,
                failedRows
            });
        }

        // ---- Helper: phát hiện định dạng ảnh từ magic bytes ----
        private static string DetectImageExtension(byte[] data)
        {
            if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8) return ".jpg";
            if (data.Length >= 8 && data[0] == 0x89 && data[1] == 0x50) return ".png";
            if (data.Length >= 3 && data[0] == 0x47 && data[1] == 0x49) return ".gif";
            if (data.Length >= 4 && data[0] == 0x52 && data[1] == 0x49) return ".webp";
            return ".jpg";
        }
    }

    // ================================================================
    // DTO / Models nội bộ
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
        public string Status { get; set; } = "New"; // New | Update | Error
        public string? ErrorMessage { get; set; }
        public List<string> Warnings { get; set; } = new();
        public bool HasImage { get; set; }
        public string? ImageThumbnailBase64 { get; set; }
        public byte[]? ImageBytesForCache { get; set; } // NOT serialized to JSON
    }

    public class ImportConfirmRequest
    {
        public string ImportSessionId { get; set; } = "";
        public List<int> ConfirmedRowIndexes { get; set; } = new();
    }
}
