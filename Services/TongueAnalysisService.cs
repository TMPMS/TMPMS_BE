using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using TMPMS.Data;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    // Phân tích ảnh lưỡi bằng Gemini Vision (Thiệt chẩn) — bổ trợ cho kết quả tự chẩn đoán theo
    // bảng câu hỏi (Vấn chẩn) ở DiagnosisService.ClassifyAsync. Cùng pattern đa model + fallback
    // tĩnh như MeridianAnalysisService/PrescriptionOcrService.
    public class TongueAnalysisService : ITongueAnalysisService
    {
        private readonly TMPMSDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TongueAnalysisService> _logger;

        private static readonly string[] ModelsToTry = { "gemini-2.5-flash", "gemini-3.5-flash-lite", "gemini-2.0-flash", "gemini-flash-latest" };

        public TongueAnalysisService(TMPMSDbContext context, HttpClient httpClient, IConfiguration configuration, ILogger<TongueAnalysisService> logger)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<TongueAnalysisResponseDto> AnalyzeAsync(byte[] imageBytes, string mimeType)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return GetFallback("Không nhận được ảnh hợp lệ.");

            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return GetFallback("Tính năng phân tích ảnh lưỡi bằng AI hiện chưa khả dụng.");

            // Feed danh sách thể bệnh thật trong DB vào prompt để AI gợi ý đúng tên đã có sẵn,
            // thay vì tự bịa tên thể bệnh không khớp với dữ liệu hệ thống đang quản lý.
            var syndromeNames = await _context.SyndromeTypes.Select(s => s.Name).ToListAsync();

            var result = await TryGeminiAnalysis(imageBytes, mimeType, syndromeNames, apiKey);
            return result ?? GetFallback("Không thể phân tích ảnh bằng AI vào lúc này. Vui lòng thử lại sau hoặc đặt lịch khám trực tiếp để được chẩn đoán chính xác.");
        }

        private async Task<TongueAnalysisResponseDto?> TryGeminiAnalysis(byte[] imageBytes, string mimeType, List<string> syndromeNames, string apiKey)
        {
            try
            {
                var syndromeList = syndromeNames.Count > 0
                    ? string.Join(", ", syndromeNames)
                    : "Khí hư, Huyết hư, Âm hư, Dương hư, Đàm thấp";

                string prompt = $@"Bạn là chuyên gia Y học cổ truyền (Đông Y), thực hiện Thiệt chẩn (xem lưỡi) — một trong Tứ chẩn (Vọng/Văn/Vấn/Thiết). Hãy quan sát ảnh lưỡi được cung cấp và mô tả:
- Sắc chất lưỡi (màu lưỡi: đỏ nhạt/đỏ sẫm/nhợt/tím...)
- Màu rêu lưỡi (trắng/vàng/xám/không rêu...)
- Độ dày rêu lưỡi (mỏng/dày/bong tróc...)
- Độ ẩm (khô/ướt/nhuận...)

Nếu ảnh KHÔNG phải ảnh lưỡi người hoặc chất lượng quá kém để quan sát, hãy trả observations mô tả rõ điều đó và để relatedSyndromes là mảng rỗng.

Chỉ được chọn relatedSyndromes (0-2 mục, gợi ý chứ không khẳng định) từ đúng danh sách thể bệnh sau, không tự tạo tên khác: {syndromeList}.

Trả lời CHỈ bằng JSON theo đúng schema, không thêm giải thích, không markdown:
{{
  ""tongueColor"": ""..."",
  ""coatingColor"": ""..."",
  ""coatingThickness"": ""..."",
  ""moisture"": ""..."",
  ""observations"": ""Mô tả tổng quát 1-2 câu"",
  ""relatedSyndromes"": [""...""],
  ""recommendation"": ""Lời khuyên sinh hoạt ngắn gọn, kèm khuyến nghị đặt lịch khám nếu cần""
}}";

                var base64 = Convert.ToBase64String(imageBytes);
                var payload = new
                {
                    contents = new object[]
                    {
                        new
                        {
                            role = "user",
                            parts = new object[]
                            {
                                new { text = prompt },
                                new { inlineData = new { mimeType, data = base64 } }
                            }
                        }
                    },
                    generationConfig = new { responseMimeType = "application/json" }
                };

                var body = JsonSerializer.Serialize(payload);
                HttpResponseMessage? response = null;
                string responseString = "";

                foreach (var model in ModelsToTry)
                {
                    try
                    {
                        var req = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent")
                        {
                            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                        };
                        req.Headers.Add("x-goog-api-key", apiKey);
                        var res = await _httpClient.SendAsync(req);
                        var resBody = await res.Content.ReadAsStringAsync();
                        if (res.IsSuccessStatusCode)
                        {
                            response = res;
                            responseString = resBody;
                            break;
                        }
                        else if (response == null)
                        {
                            response = res;
                            responseString = resBody;
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
                    _logger.LogWarning("TongueAnalysis Gemini error: {Body}", truncated);
                    return null;
                }

                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;
                if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0) return null;
                if (!candidates[0].TryGetProperty("content", out var content) ||
                    !content.TryGetProperty("parts", out var parts) ||
                    parts.GetArrayLength() == 0 ||
                    !parts[0].TryGetProperty("text", out var textProp)) return null;

                var aiJsonText = textProp.GetString();
                if (string.IsNullOrEmpty(aiJsonText)) return null;

                using var aiDoc = JsonDocument.Parse(aiJsonText);
                var aiRoot = aiDoc.RootElement;

                var dto = new TongueAnalysisResponseDto
                {
                    TongueColor = GetString(aiRoot, "tongueColor"),
                    CoatingColor = GetString(aiRoot, "coatingColor"),
                    CoatingThickness = GetString(aiRoot, "coatingThickness"),
                    Moisture = GetString(aiRoot, "moisture"),
                    Observations = GetString(aiRoot, "observations"),
                    Recommendation = GetString(aiRoot, "recommendation"),
                    IsAiGenerated = true
                };

                if (aiRoot.TryGetProperty("relatedSyndromes", out var syn) && syn.ValueKind == JsonValueKind.Array)
                {
                    dto.RelatedSyndromes = syn.EnumerateArray()
                        .Select(s => s.ValueKind == JsonValueKind.String ? s.GetString() ?? "" : "")
                        .Where(s => !string.IsNullOrWhiteSpace(s) && syndromeNames.Contains(s))
                        .ToList();
                }

                if (string.IsNullOrWhiteSpace(dto.Observations)) return null;

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TongueAnalysis Gemini call failed");
                return null;
            }
        }

        private static string GetString(JsonElement root, string prop)
            => root.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

        private static TongueAnalysisResponseDto GetFallback(string disclaimerSuffix)
        {
            return new TongueAnalysisResponseDto
            {
                TongueColor = "",
                CoatingColor = "",
                CoatingThickness = "",
                Moisture = "",
                Observations = "Chưa thể phân tích ảnh vào lúc này.",
                RelatedSyndromes = new List<string>(),
                Recommendation = "Vui lòng thử lại sau hoặc đặt lịch khám trực tiếp để được Dược sĩ/bác sĩ thăm khám chính xác.",
                IsAiGenerated = false,
                Disclaimer = disclaimerSuffix
            };
        }
    }
}
