using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using TMPMS.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

using System.Net.Http;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Configuration;
using Services.Interfaces;
using TMPMS.Utils;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("[controller]")]
    public class MedicinesController : ControllerBase
    {
        private readonly IMedicineService _service;
        private readonly IConfiguration _config;
        private readonly IMedicineImageSearchService _imageSearchService;
        private readonly IAuditLogService _auditLogService;

        public MedicinesController(IMedicineService service, IConfiguration config, IMedicineImageSearchService imageSearchService, IAuditLogService auditLogService)
        {
            _service = service;
            _config = config;
            _imageSearchService = imageSearchService;
            _auditLogService = auditLogService;
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
            var bytes = ms.ToArray();
            if (!TMPMS.Utils.ImageMagicBytes.LooksLikeImage(bytes))
                return BadRequest(new { message = "Tệp không phải ảnh hợp lệ." });
            var mimeType = ext switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };

            var keyword = await _imageSearchService.IdentifyAsync(bytes, mimeType);
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
            var filter = new MedicineSearchFilterDto
            {
                CategoryIdStr = categoryIdStr,
                SupplierIdStr = supplierIdStr,
                Origin = originStr,
                Unit = unitStr,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Name = nameStr,
                IncludeRx = includeRx,
                InStock = inStock,
                HasDiscount = hasDiscount,
                PartUsed = partUsedStr,
                Effects = effectsStr,
                HerbalOnly = herbalOnly,
                Sort = sort,
                Page = page,
                PageSize = pageSize
            };

            var (items, totalCount, isPaged) = await _service.SearchAsync(filter);

            // Giữ nguyên contract cũ (trả mảng thô) khi KHÔNG phân trang, để không phá các FE caller
            // hiện có (Array.isArray(data)). Chỉ khi có page/page_size mới bọc thêm metadata.
            if (!isPaged)
            {
                return Ok(items);
            }

            return Ok(new { items, totalCount, page = page!.Value, pageSize = pageSize!.Value });
        }

        // Danh sách giá trị "Bộ phận dùng" / "Công dụng" hiện có, dùng cho dropdown filter Đông y ở FE.
        [HttpGet("herbal-filter-options")]
        public async Task<IActionResult> GetHerbalFilterOptions()
        {
            var options = await _service.GetHerbalFilterOptionsAsync();
            return Ok(new { partUsed = options.PartUsed, effects = options.Effects });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMedicineById(int id)
        {
            var m = await _service.GetByIdAsync(id);
            if (m == null) return NotFound();
            return Ok(m);
        }

        // Tra cứu nhanh 1 sản phẩm theo mã vạch — dùng cho máy quét barcode ở POS/kiểm kê.
        [HttpGet("by-barcode/{barcode}")]
        public async Task<IActionResult> GetMedicineByBarcode(string barcode)
        {
            var m = await _service.GetByBarcodeAsync(barcode);
            if (m == null) return NotFound(new { message = "Không tìm thấy sản phẩm với mã vạch này" });
            return Ok(m);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> AddMedicine([FromBody] MedicineCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var medicine = await _service.CreateAsync(dto);

            await this.LogAuditAsync(_auditLogService, "Medicine", "Create", medicine.Id.ToString(), $"Tạo dược phẩm '{medicine.Name}' (#{medicine.Id})");

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

            var updated = await _service.UpdateAsync(medId, dto);
            if (updated == null) return NotFound(new { error = "Không tìm thấy dược phẩm" });

            await this.LogAuditAsync(_auditLogService, "Medicine", "Update", updated.Id.ToString(), $"Cập nhật dược phẩm '{updated.Name}' (#{updated.Id})");

            return Ok(updated);
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

            var (found, deactivated, name) = await _service.DeleteAsync(medId);
            if (!found) return NotFound(new { error = "Không tìm thấy dược phẩm" });

            if (deactivated)
            {
                await this.LogAuditAsync(_auditLogService, "Medicine", "Deactivate", medId.ToString(), $"Ẩn dược phẩm '{name}' (#{medId}) — có liên kết đơn hàng/lô hàng nên không xóa hẳn");
            }
            else
            {
                await this.LogAuditAsync(_auditLogService, "Medicine", "Delete", medId.ToString(), $"Xóa hẳn dược phẩm '{name}' (#{medId})");
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
                        var req = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent")
                        {
                            Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
                        };
                        req.Headers.Add("x-goog-api-key", apiKey);
                        var res = await client.SendAsync(req);
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
