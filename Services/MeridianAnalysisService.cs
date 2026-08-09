using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using TMPMS.Data;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    public class MeridianAnalysisService : IMeridianAnalysisService
    {
        private readonly TMPMSDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MeridianAnalysisService> _logger;

        private const int AiCacheDurationDays = 30;
        private const int FallbackCacheDurationHours = 1;

        private static readonly string[] MeridianCodes = { "PHE", "THAN", "TY", "CAN", "TAM", "VI" };
        private static readonly string[] ModelsToTry = { "gemini-2.5-flash", "gemini-3.5-flash-lite", "gemini-2.0-flash", "gemini-flash-latest" };

        public MeridianAnalysisService(TMPMSDbContext context, HttpClient httpClient, IMemoryCache cache, IConfiguration configuration, ILogger<MeridianAnalysisService> logger)
        {
            _context = context;
            _httpClient = httpClient;
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<MeridianAnalysisResponseDto> GetAsync(int medicineId)
        {
            string cacheKey = $"MERIDIAN_AI_{medicineId}";
            if (_cache.TryGetValue(cacheKey, out MeridianAnalysisResponseDto? cached) && cached != null)
            {
                return cached;
            }

            var medicine = await _context.Medicines.FirstOrDefaultAsync(m => m.Id == medicineId);
            if (medicine == null)
            {
                return GetFallback("Không tìm thấy sản phẩm.");
            }

            var herbalInfo = await _context.HerbalMedicineInfos.FirstOrDefaultAsync(h => h.MedicineId == medicineId);

            var apiKey = _configuration["Gemini:ApiKey"];
            MeridianAnalysisResponseDto? result = null;

            if (!string.IsNullOrEmpty(apiKey))
            {
                result = await TryGeminiAnalysis(medicine.Name, medicine.Description, herbalInfo?.Properties, herbalInfo?.Effects, apiKey);
            }

            result ??= GetFallback("Không thể phân tích bằng AI vào lúc này. Đây là dữ liệu tổng quát mặc định, chưa được kiểm chứng cho sản phẩm cụ thể này.");

            var cacheDuration = result.IsAiGenerated
                ? TimeSpan.FromDays(AiCacheDurationDays)
                : TimeSpan.FromHours(FallbackCacheDurationHours);
            _cache.Set(cacheKey, result, cacheDuration);
            return result;
        }

        private async Task<MeridianAnalysisResponseDto?> TryGeminiAnalysis(string name, string? description, string? properties, string? effects, string apiKey)
        {
            try
            {
                string systemPrompt = $@"Bạn là chuyên gia Y học cổ truyền (Đông Y). Dựa trên tên và mô tả vị thuốc/bài thuốc dưới đây, hãy xác định Tính vị, các Đường Kinh mà vị thuốc quy vào, Công năng chủ trị, và 2-3 Huyệt vị bổ trợ liên quan.

Tên vị thuốc: {name}
Mô tả: {description}
Tính vị đã biết (nếu có): {properties}
Công dụng đã biết (nếu có): {effects}

CHỈ được dùng các mã kinh sau (không dùng mã khác): PHE, THAN, TY, CAN, TAM, VI.

Với mỗi huyệt vị, hãy trả về tọa độ 3D quy ước [x, y, z] trên mô hình cơ thể người đứng thẳng:
- x: khoảng cách ngang từ trục giữa cơ thể, từ 0 (giữa) đến khoảng 0.7 (ngoài cùng cánh tay)
- y: độ cao từ chân (khoảng -1.8) đến đỉnh đầu (khoảng 1.8), 0 là ngang rốn
- z: độ sâu trước/sau, từ -0.3 (sau lưng) đến 0.3 (phía trước bụng)

Ví dụ: Túc Tam Lý (ST36) ở khoảng [0.32, -0.65, 0.15], Bách Hội (GV20) ở khoảng [0.0, 1.68, 0.0].

Trả lời CHỈ bằng JSON theo đúng schema sau, không thêm giải thích:
{{
  ""nature"": ""Vị ... tính ..."",
  ""meridians"": [""PHE"", ""THAN""],
  ""functions"": ""Mô tả công năng..."",
  ""acupoints"": [
    {{ ""name"": ""Tên huyệt (Mã riêng của huyệt, ví dụ BL23)"", ""code"": ""Mã riêng của huyệt đó — VÍ DỤ: BL23, ST36, SP6 — KHÔNG được lặp lại mã đường kinh ở trên"", ""location"": ""Vị trí giải phẫu"", ""benefit"": ""Tác dụng"", ""position"": [0.0, 0.0, 0.0] }}
  ]
}}";

                var payload = new
                {
                    systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                    contents = new[]
                    {
                        new { role = "user", parts = new[] { new { text = $"Phân tích quy kinh & huyệt vị cho: {name}" } } }
                    },
                    generationConfig = new { responseMimeType = "application/json" }
                };

                HttpResponseMessage? response = null;
                string responseString = "";

                foreach (var model in ModelsToTry)
                {
                    try
                    {
                        var req = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent")
                        {
                            Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
                        };
                        req.Headers.Add("x-goog-api-key", apiKey);
                        var res = await _httpClient.SendAsync(req);
                        var body = await res.Content.ReadAsStringAsync();
                        if (res.IsSuccessStatusCode)
                        {
                            response = res;
                            responseString = body;
                            break;
                        }
                        else if (response == null)
                        {
                            response = res;
                            responseString = body;
                        }
                    }
                    catch (Exception modelEx)
                    {
                        if (response == null) responseString = modelEx.Message;
                    }
                }

                if (response == null || !response.IsSuccessStatusCode)
                {
                    var truncated = responseString.Length > 500 ? responseString.Substring(0, 500) + "..." : responseString;
                    _logger.LogWarning("MeridianAnalysis Gemini error: {Body}", truncated);
                    return null;
                }

                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;
                if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                    return null;

                if (!candidates[0].TryGetProperty("content", out var content) ||
                    !content.TryGetProperty("parts", out var parts) ||
                    parts.GetArrayLength() == 0 ||
                    !parts[0].TryGetProperty("text", out var textProp))
                    return null;

                var aiJsonText = textProp.GetString();
                if (string.IsNullOrEmpty(aiJsonText)) return null;

                using var aiDoc = JsonDocument.Parse(aiJsonText);
                var aiRoot = aiDoc.RootElement;

                var dto = new MeridianAnalysisResponseDto
                {
                    Nature = aiRoot.TryGetProperty("nature", out var natureProp) ? natureProp.GetString() ?? "" : "",
                    Functions = aiRoot.TryGetProperty("functions", out var funcProp) ? funcProp.GetString() ?? "" : "",
                    IsAiGenerated = true
                };

                if (aiRoot.TryGetProperty("meridians", out var meridiansProp) && meridiansProp.ValueKind == JsonValueKind.Array)
                {
                    dto.Meridians = meridiansProp.EnumerateArray()
                        .Select(m => m.GetString() ?? "")
                        .Where(m => MeridianCodes.Contains(m))
                        .ToList();
                }

                if (aiRoot.TryGetProperty("acupoints", out var acupointsProp) && acupointsProp.ValueKind == JsonValueKind.Array)
                {
                    dto.Acupoints = acupointsProp.EnumerateArray().Select(a => new AcupointDto
                    {
                        Name = a.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        Code = a.TryGetProperty("code", out var c) ? c.GetString() ?? "" : "",
                        Location = a.TryGetProperty("location", out var l) ? l.GetString() ?? "" : "",
                        Benefit = a.TryGetProperty("benefit", out var b) ? b.GetString() ?? "" : "",
                        Position = a.TryGetProperty("position", out var p)
                            ? ParsePosition(p)
                            : new double[] { 0, 0, 0 }
                    }).ToList();
                }

                // Không đủ dữ liệu hợp lệ (AI trả sai schema) — để caller rơi về fallback thay vì hiển thị modal trống.
                if (dto.Meridians.Count == 0 || dto.Acupoints.Count == 0) return null;

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MeridianAnalysis Gemini call failed");
                return null;
            }
        }

        // AI đôi khi trả sai định dạng cho một tọa độ đơn lẻ (vd lồng mảng) — dung túng lỗi ở từng
        // phần tử thay vì làm hỏng toàn bộ kết quả phân tích chỉ vì một huyệt vị có dữ liệu sai.
        private static double[] ParsePosition(JsonElement p)
        {
            if (p.ValueKind != JsonValueKind.Array) return new double[] { 0, 0, 0 };
            var values = p.EnumerateArray()
                .Select(x => x.ValueKind == JsonValueKind.Number && x.TryGetDouble(out var d) ? d : 0)
                .ToArray();
            if (values.Length == 3) return values;
            var result = new double[3];
            Array.Copy(values, result, Math.Min(values.Length, 3));
            return result;
        }

        private static MeridianAnalysisResponseDto GetFallback(string disclaimer)
        {
            return new MeridianAnalysisResponseDto
            {
                Nature = "Vị ngọt đắng nhẹ, tính bình, bổ dưỡng cơ thể",
                Meridians = new List<string> { "PHE", "TY", "THAN" },
                Functions = "Hỗ trợ tăng cường sức khỏe, dưỡng âm bổ khí, cân bằng tạng phủ.",
                Acupoints = new List<AcupointDto>
                {
                    new AcupointDto { Name = "Túc Tam Lý (ST36)", Code = "ST36", Location = "Dưới lõm ngoài xương bánh chè 3 thốn", Benefit = "Đại bổ nguyên khí, tăng cường tiêu hóa & miễn dịch.", Position = new double[] { 0.32, -0.65, 0.15 } },
                    new AcupointDto { Name = "Tam Âm Giao (SP6)", Code = "SP6", Location = "Trên đỉnh mắt cá trong 3 thốn", Benefit = "Bổ Tỳ Can Thận, dưỡng huyết bổ khí.", Position = new double[] { 0.22, -1.05, 0.12 } }
                },
                IsAiGenerated = false,
                Disclaimer = disclaimer
            };
        }
    }
}
