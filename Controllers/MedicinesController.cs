using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using TMPMS.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

using System.Net.Http;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Configuration;
using Services.Interfaces;
using TMPMS.DTOs;
using TMPMS.Utils;

namespace TMPMS.Controllers
{
    public class MedicineUpdateDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public decimal? OldPrice { get; set; }
        public int? StockQuantity { get; set; }
        public string? Unit { get; set; }
        public string? Origin { get; set; }
        public string? Packaging { get; set; }
        public string? ImageUrl { get; set; }
        public bool? RequiresPrescription { get; set; }
        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Route("[controller]")]
    public class MedicinesController : ControllerBase
    {
        private readonly TMPMSDbContext _context;
        private readonly IConfiguration _config;
        private readonly IMedicineImageSearchService _imageSearchService;

        public MedicinesController(TMPMSDbContext context, IConfiguration config, IMedicineImageSearchService imageSearchService)
        {
            _context = context;
            _config = config;
            _imageSearchService = imageSearchService;
        }

        // Khách hàng dán/tải ảnh vỏ thuốc ở ô tìm kiếm bằng hình ảnh — AI đọc tên sản phẩm trên bao
        // bì rồi trả về dạng từ khoá để FE tái sử dụng luôn pipeline tìm kiếm theo tên đã có sẵn.
        [HttpPost("search-by-image")]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> SearchByImage(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest(new { message = "Vui lòng chọn ảnh." });
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) return BadRequest(new { message = "Chỉ hỗ trợ JPG, PNG hoặc WEBP." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var mimeType = ext switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };

            var keyword = await _imageSearchService.IdentifyAsync(ms.ToArray(), mimeType);
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Ok(new MedicineImageSearchResultDto
                {
                    Keyword = null,
                    IsAiGenerated = false,
                    Disclaimer = "Không nhận diện được sản phẩm trong ảnh. Vui lòng thử ảnh rõ nét hơn hoặc tìm bằng tên."
                });
            }

            return Ok(new MedicineImageSearchResultDto { Keyword = keyword, IsAiGenerated = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetMedicines(
            [FromQuery(Name = "category_id")] string? categoryIdStr,
            [FromQuery(Name = "supplier_id")] string? supplierIdStr,
            [FromQuery(Name = "origin")] string? originStr,
            [FromQuery(Name = "unit")] string? unitStr,
            [FromQuery(Name = "min_price")] decimal? minPrice,
            [FromQuery(Name = "max_price")] decimal? maxPrice,
            [FromQuery(Name = "name")] string? nameStr,
            [FromQuery(Name = "include_rx")] bool includeRx = false,
            [FromQuery(Name = "in_stock")] bool? inStock = null,
            [FromQuery(Name = "has_discount")] bool? hasDiscount = null,
            [FromQuery(Name = "part_used")] string? partUsedStr = null,
            [FromQuery(Name = "effects")] string? effectsStr = null,
            [FromQuery(Name = "herbal_only")] bool? herbalOnly = null,
            [FromQuery(Name = "sort")] string? sort = null,
            [FromQuery(Name = "page")] int? page = null,
            [FromQuery(Name = "page_size")] int? pageSize = null)
        {
            var query = _context.Medicines.Where(m => m.IsActive).AsQueryable();

            if (!includeRx)
            {
                query = query.Where(m => !m.RequiresPrescription);
            }

            if (!string.IsNullOrEmpty(categoryIdStr))
            {
                var cleanId = categoryIdStr.Replace("eq.", "");
                if (int.TryParse(cleanId, out int catId))
                {
                    query = query.Where(m => m.CategoryId == catId);
                }
            }

            if (!string.IsNullOrEmpty(supplierIdStr))
            {
                var cleanId = supplierIdStr.Replace("eq.", "");
                if (int.TryParse(cleanId, out int supId))
                {
                    query = query.Where(m => m.SupplierId == supId);
                }
            }

            if (!string.IsNullOrWhiteSpace(originStr))
            {
                query = query.Where(m => m.Origin != null && m.Origin.Contains(originStr.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(unitStr))
            {
                query = query.Where(m => m.Unit != null && m.Unit.Contains(unitStr.Trim()));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(m => m.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(m => m.Price <= maxPrice.Value);
            }

            if (!string.IsNullOrEmpty(nameStr))
            {
                var searchTerm = Uri.UnescapeDataString(nameStr)
                    .Replace("ilike.*", "")
                    .Replace("*", "")
                    .Trim();

                // Khớp theo từng từ thay vì cả cụm nguyên văn: tên do AI đọc từ ảnh (hoặc gõ tay)
                // thường khác thứ tự/thêm bớt từ so với tên lưu trong danh mục (vd "Mật Ong Rừng
                // Tây Nguyên" khi tên thật là "Mật ong hoa rừng nguyên chất Tây Nguyên"), nên yêu
                // cầu khớp nguyên cụm sẽ bỏ sót sản phẩm dù đọc đúng nhãn hàng.
                var words = searchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    foreach (var word in words)
                    {
                        var w = word;
                        query = query.Where(m => m.Name.Contains(w) || (m.Description != null && m.Description.Contains(w)));
                    }
                }
                else
                {
                    query = query.Where(m => m.Name.Contains(searchTerm) || (m.Description != null && m.Description.Contains(searchTerm)));
                }
            }

            if (inStock == true)
            {
                query = query.Where(m => m.StockQuantity > 0);
            }

            if (hasDiscount == true)
            {
                query = query.Where(m => m.Discount != null && m.Discount > 0);
            }

            var partUsed = string.IsNullOrWhiteSpace(partUsedStr) ? null : partUsedStr.Trim();
            var effects = string.IsNullOrWhiteSpace(effectsStr) ? null : effectsStr.Trim();
            if (herbalOnly == true || partUsed != null || effects != null)
            {
                query = query.Where(m => _context.HerbalMedicineInfos.Any(h => h.MedicineId == m.Id
                    && (partUsed == null || h.PartUsed.Contains(partUsed))
                    && (effects == null || h.Effects.Contains(effects))));
            }

            // Đếm tổng số kết quả TRƯỚC khi phân trang, để FE hiển thị "còn bao nhiêu" / nút tải thêm.
            var totalCount = await query.CountAsync();

            query = sort switch
            {
                "priceAsc" => query.OrderBy(m => m.Price),
                "priceDesc" => query.OrderByDescending(m => m.Price),
                "nameAsc" => query.OrderBy(m => m.Name),
                "nameDesc" => query.OrderByDescending(m => m.Name),
                "newest" => query.OrderByDescending(m => m.CreatedAt),
                _ => query.OrderBy(m => m.Id)
            };

            // Phân trang là OPT-IN: chỉ áp dụng khi FE gửi page/page_size, để không phá các nơi
            // (vd. trang Admin) vẫn cần tải toàn bộ danh sách thuốc trong 1 lần gọi.
            var isPaged = page.HasValue && pageSize.HasValue && pageSize.Value > 0;
            if (isPaged)
            {
                query = query.Skip((page!.Value - 1) * pageSize!.Value).Take(pageSize.Value);
            }

            var medicines = await query.ToListAsync();
            var medIds = medicines.Select(m => m.Id).ToList();

            var reviewStats = await _context.Reviews
                .Where(r => medIds.Contains(r.MedicineId))
                .GroupBy(r => r.MedicineId)
                .Select(g => new {
                    MedicineId = g.Key,
                    AvgRating = g.Average(r => (double)r.Rating),
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.MedicineId);

            var response = medicines.Select(m => {
                double avgRating = reviewStats.TryGetValue(m.Id, out var stat) && stat.Count > 0
                    ? Math.Round(stat.AvgRating, 1)
                    : Math.Round(4.3 + (m.Id % 8) / 10.0, 1);

                return new {
                    m.Id,
                    m.CategoryId,
                    m.SupplierId,
                    m.Name,
                    m.Description,
                    m.Price,
                    PriceStatus = m.Price == null ? "contact" : "available",
                    m.StockQuantity,
                    m.ManufactureDate,
                    m.ExpiryDate,
                    m.RequiresPrescription,
                    m.ImageUrl,
                    m.Unit,
                    m.Origin,
                    m.Packaging,
                    m.OldPrice,
                    m.Discount,
                    m.IsActive,
                    m.CreatedAt,
                    Rating = avgRating,
                    ReviewCount = reviewStats.TryGetValue(m.Id, out var st) ? st.Count : 0
                };
            });

            // Giữ nguyên contract cũ (trả mảng thô) khi KHÔNG phân trang, để không phá các FE caller
            // hiện có (Array.isArray(data)). Chỉ khi có page/page_size mới bọc thêm metadata.
            if (!isPaged)
            {
                return Ok(response);
            }

            return Ok(new { items = response, totalCount, page = page!.Value, pageSize = pageSize!.Value });
        }

        // Danh sách giá trị "Bộ phận dùng" / "Công dụng" hiện có, dùng cho dropdown filter Đông y ở FE.
        [HttpGet("herbal-filter-options")]
        public async Task<IActionResult> GetHerbalFilterOptions()
        {
            var partUsed = await _context.HerbalMedicineInfos
                .Where(h => h.PartUsed != null && h.PartUsed != "")
                .Select(h => h.PartUsed)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var effects = await _context.HerbalMedicineInfos
                .Where(h => h.Effects != null && h.Effects != "")
                .Select(h => h.Effects)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return Ok(new { partUsed, effects });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMedicineById(int id)
        {
            var m = await _context.Medicines.FindAsync(id);
            if (m == null) return NotFound();

            return Ok(new {
                m.Id,
                m.CategoryId,
                m.SupplierId,
                m.Name,
                m.Description,
                m.Price,
                PriceStatus = m.Price == null ? "contact" : "available",
                m.StockQuantity,
                m.ManufactureDate,
                m.ExpiryDate,
                m.RequiresPrescription,
                m.ImageUrl,
                m.Unit,
                m.Origin,
                m.Packaging,
                m.OldPrice,
                m.Discount,
                m.IsActive,
                m.CreatedAt
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> AddMedicine([FromBody] Medicine medicine)
        {
            medicine.CreatedAt = DateTime.UtcNow;
            medicine.IsActive = true;
            if (medicine.ManufactureDate == default) medicine.ManufactureDate = DateTime.UtcNow;
            if (medicine.ExpiryDate == default) medicine.ExpiryDate = DateTime.UtcNow.AddYears(1);
            // Tồn kho chỉ được cộng qua nhập lô (StockBatch) — sản phẩm mới luôn bắt đầu từ 0
            // và cần nhập lô đầu tiên (số lô, NSX, HSD thật) trong tab Nhập kho.
            medicine.StockQuantity = 0;

            _context.Medicines.Add(medicine);
            await _context.SaveChangesAsync();

            return StatusCode(201, medicine);
        }

        [HttpPut("{id}")]
        [HttpPatch("{id}")]
        [HttpPut]
        [HttpPatch]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> UpdateMedicine(
            [FromRoute] int? id,
            [FromQuery(Name = "id")] string? idQuery,
            [FromBody] MedicineUpdateDto dto)
        {
            int medId = id ?? 0;
            if (medId == 0 && !string.IsNullOrEmpty(idQuery))
            {
                var clean = idQuery.Replace("eq.", "");
                int.TryParse(clean, out medId);
            }

            var med = await _context.Medicines.FindAsync(medId);
            if (med == null) return NotFound(new { error = "Không tìm thấy dược phẩm" });

            if (!string.IsNullOrWhiteSpace(dto.Name)) med.Name = dto.Name;
            if (dto.Description != null) med.Description = dto.Description;
            if (dto.Price != null) med.Price = dto.Price;
            if (dto.OldPrice != null) med.OldPrice = dto.OldPrice;
            // Số lượng tồn kho không còn được sửa trực tiếp ở đây — nguồn sự thật là StockBatches,
            // chỉnh qua API /api/inventory/batches (nhập lô mới / hủy / kiểm kê điều chỉnh).
            if (!string.IsNullOrWhiteSpace(dto.Unit)) med.Unit = dto.Unit;
            if (!string.IsNullOrWhiteSpace(dto.Origin)) med.Origin = dto.Origin;
            if (!string.IsNullOrWhiteSpace(dto.Packaging)) med.Packaging = dto.Packaging;
            if (!string.IsNullOrWhiteSpace(dto.ImageUrl)) med.ImageUrl = dto.ImageUrl;
            if (dto.RequiresPrescription != null) med.RequiresPrescription = dto.RequiresPrescription.Value;
            if (dto.CategoryId != null && dto.CategoryId > 0) med.CategoryId = dto.CategoryId.Value;
            if (dto.SupplierId != null && dto.SupplierId > 0) med.SupplierId = dto.SupplierId.Value;

            await _context.SaveChangesAsync();

            return Ok(new {
                med.Id,
                med.CategoryId,
                med.SupplierId,
                med.Name,
                med.Description,
                med.Price,
                med.StockQuantity,
                med.RequiresPrescription,
                med.ImageUrl,
                med.Unit,
                med.Origin,
                med.Packaging,
                med.OldPrice,
                med.IsActive
            });
        }

        [HttpDelete("{id}")]
        [HttpDelete]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> DeleteMedicine(
            [FromRoute] int? id,
            [FromQuery(Name = "id")] string? idQuery)
        {
            int medId = id ?? 0;
            if (medId == 0 && !string.IsNullOrEmpty(idQuery))
            {
                var clean = idQuery.Replace("eq.", "");
                int.TryParse(clean, out medId);
            }

            var med = await _context.Medicines.FindAsync(medId);
            if (med == null) return NotFound(new { error = "Không tìm thấy dược phẩm" });

            bool hasLinks = await _context.OrderItems.AnyAsync(oi => oi.MedicineId == medId) ||
                            await _context.CartItems.AnyAsync(ci => ci.MedicineId == medId) ||
                            await _context.PrescriptionItems.AnyAsync(pi => pi.MedicineId == medId) ||
                            await _context.StockBatches.AnyAsync(b => b.MedicineId == medId);

            if (hasLinks)
            {
                med.IsActive = false;
                await _context.SaveChangesAsync();
            }
            else
            {
                _context.Medicines.Remove(med);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Đã xóa hoặc ẩn dược phẩm thành công" });
        }

        [HttpPost("verify-image")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> VerifyMedicineImage([FromBody] MedicineImageVerifyRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ProductName))
            {
                return BadRequest(new { error = "Tên sản phẩm không được để trống" });
            }

            if (string.IsNullOrWhiteSpace(dto.ImageUrl) && string.IsNullOrWhiteSpace(dto.ImageBase64))
            {
                return BadRequest(new { error = "Cần cung cấp URL hình ảnh hoặc dữ liệu ảnh dạng Base64" });
            }

            var apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { error = "Chưa cấu hình API Key cho Gemini" });
            }

            try
            {
                string base64Image = "";
                string mimeType = "image/jpeg";

                if (!string.IsNullOrWhiteSpace(dto.ImageBase64))
                {
                    var parts = dto.ImageBase64.Split(",");
                    if (parts.Length > 1)
                    {
                        if (parts[0].Contains("image/png")) mimeType = "image/png";
                        else if (parts[0].Contains("image/webp")) mimeType = "image/webp";
                        base64Image = parts[1];
                    }
                    else
                    {
                        base64Image = dto.ImageBase64;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
                {
                    if (!Uri.TryCreate(dto.ImageUrl, UriKind.Absolute, out var imageUri) || SsrfGuard.IsUnsafeFetchTarget(imageUri))
                    {
                        return BadRequest(new { error = "URL ảnh không hợp lệ hoặc trỏ tới địa chỉ nội bộ không được phép." });
                    }

                    // Dùng HttpClientHandler mặc định — KHÔNG bỏ qua lỗi xác thực chứng chỉ TLS
                    // (trước đây ServerCertificateCustomValidationCallback luôn trả true, dễ bị MITM).
                    using var http = new HttpClient();
                    http.Timeout = TimeSpan.FromSeconds(15);
                    http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                    http.DefaultRequestHeaders.Add("Accept", "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8");

                    try
                    {
                        const int maxImageBytes = 5 * 1024 * 1024; // 5MB — khớp giới hạn dùng trong ProductImportController
                        using var imgResponse = await http.GetAsync(imageUri, HttpCompletionOption.ResponseHeadersRead);
                        imgResponse.EnsureSuccessStatusCode();
                        if (imgResponse.Content.Headers.ContentLength.HasValue && imgResponse.Content.Headers.ContentLength.Value > maxImageBytes)
                        {
                            return BadRequest(new { error = "Ảnh vượt quá dung lượng cho phép (tối đa 5MB)." });
                        }

                        using var contentStream = await imgResponse.Content.ReadAsStreamAsync();
                        using var ms = new MemoryStream();
                        var buffer = new byte[81920];
                        int read;
                        long total = 0;
                        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            total += read;
                            if (total > maxImageBytes)
                            {
                                return BadRequest(new { error = "Ảnh vượt quá dung lượng cho phép (tối đa 5MB)." });
                            }
                            ms.Write(buffer, 0, read);
                        }

                        var bytes = ms.ToArray();
                        base64Image = Convert.ToBase64String(bytes);

                        var lowerUrl = dto.ImageUrl.ToLower();
                        if (lowerUrl.Contains(".png")) mimeType = "image/png";
                        else if (lowerUrl.Contains(".webp")) mimeType = "image/webp";
                        else if (lowerUrl.Contains(".gif")) mimeType = "image/gif";
                    }
                    catch (Exception downloadEx)
                    {
                        return BadRequest(new { error = $"Không thể tải ảnh từ URL do máy chủ ảnh chặn truy cập hoặc link hỏng ({downloadEx.Message}). Vui lòng dùng link ảnh khác!" });
                    }
                }

                string systemPrompt = @"Bạn là chuyên gia thẩm định dữ liệu sản phẩm nhà thuốc.
Nhiệm vụ của bạn là nhận diện hình ảnh bao bì/sản phẩm dược phẩm và kiểm tra xem hình ảnh đó có khớp với tên sản phẩm và mô tả do người dùng nhập hay không.

QUY TẮC PHÂN TÍCH:
1. isMedicineImage: true nếu ảnh chứa bao bì thuốc, hộp thuốc, chai/lọ thuốc, vỉ thuốc, thực phẩm chức năng, thảo dược, vị thuốc đông y hoặc thiết bị y tế. false nếu là ảnh thú cưng, người, phong cảnh, xe cộ, sản phẩm không liên quan y tế.
2. isMatch: true nếu ảnh thể hiện đúng hoặc tương đương với tên sản phẩm/hoạt chất nhập vào. (Ví dụ: Tên nhập 'Panadol Extra', ảnh ghi 'Panadol Extra' -> true. Tên nhập 'Paracetamol', ảnh ghi 'Paracetamol 500mg' -> true).
3. confidenceScore: Điểm độ tin cậy trùng khớp từ 0 đến 100.
4. detectedName: Tên thương hiệu/hoạt chất/sản phẩm mà bạn đọc/nhận diện được từ hình ảnh.
5. warningMessage: Giải thích ngắn gọn (1-2 câu bằng tiếng Việt) lý do vì sao không khớp hoặc bất thường. Nếu khớp tốt thì để rỗng hoặc ghi nhận xét ngắn.

BẮT BUỘC trả về đúng định dạng JSON chuẩn theo schema:
{
  ""isMatch"": true,
  ""isMedicineImage"": true,
  ""confidenceScore"": 90,
  ""detectedName"": ""Tên đọc được trên ảnh"",
  ""warningMessage"": ""Cảnh báo nếu có...""
}";

                var payload = new
                {
                    systemInstruction = new
                    {
                        parts = new[] { new { text = systemPrompt } }
                    },
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new object[]
                            {
                                new
                                {
                                    inline_data = new
                                    {
                                        mime_type = mimeType,
                                        data = base64Image
                                    }
                                },
                                new
                                {
                                    text = $"Tên sản phẩm nhập vào: {dto.ProductName}\nMô tả sản phẩm: {dto.Description ?? "Không có"}"
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        responseMimeType = "application/json"
                    }
                };

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(25);

                var modelsToTry = new[] { "gemini-2.5-flash", "gemini-3.5-flash-lite", "gemini-2.0-flash", "gemini-flash-latest" };
                HttpResponseMessage? response = null;
                string responseString = "";

                foreach (var model in modelsToTry)
                {
                    try
                    {
                        var res = await client.PostAsync(
                            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}",
                            new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
                        );
                        var body = await res.Content.ReadAsStringAsync();
                        if (res.IsSuccessStatusCode)
                        {
                            response = res;
                            responseString = body;
                            break;
                        }
                        else if (string.IsNullOrEmpty(responseString))
                        {
                            response = res;
                            responseString = body;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (string.IsNullOrEmpty(responseString)) responseString = ex.Message;
                    }
                }

                if (response == null || !response.IsSuccessStatusCode)
                {
                    return BadRequest(new { error = $"AI không thể phân tích ảnh: {responseString}" });
                }

                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;
                var textResult = root.GetProperty("candidates")[0]
                                     .GetProperty("content")
                                     .GetProperty("parts")[0]
                                     .GetProperty("text").GetString();

                if (string.IsNullOrWhiteSpace(textResult))
                {
                    return BadRequest(new { error = "AI không trả về kết quả hợp lệ." });
                }

                var verifyResult = JsonSerializer.Deserialize<MedicineImageVerifyResponseDto>(textResult, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(verifyResult);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Lỗi thẩm định ảnh: {ex.Message}" });
            }
        }
    }
}
