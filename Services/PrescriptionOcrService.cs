using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    // Dùng Gemini Vision để đọc ảnh toa thuốc khách hàng gửi lên (giống cơ chế phân tích quy kinh
    // bằng AI ở MeridianAnalysisService), nhằm gợi ý sẵn thông tin & danh sách thuốc cho Dược sĩ
    // duyệt thay vì phải gõ tay lại toàn bộ toa giấy.
    public class PrescriptionOcrService : IPrescriptionOcrService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PrescriptionOcrService> _logger;

        private static readonly string[] ModelsToTry = { "gemini-2.5-flash", "gemini-3.5-flash-lite", "gemini-2.0-flash", "gemini-flash-latest" };

        public PrescriptionOcrService(HttpClient httpClient, IConfiguration configuration, ILogger<PrescriptionOcrService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PrescriptionOcrRawResult?> ExtractAsync(byte[] imageBytes, string mimeType)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || imageBytes.Length == 0) return null;

            try
            {
                const string prompt = @"Bạn là trợ lý của một nhà thuốc, có nhiệm vụ đọc ảnh toa thuốc giấy do bệnh nhân chụp gửi lên.
Hãy đọc thông tin trong ảnh và trả về CHỈ một JSON theo đúng schema sau, không thêm giải thích, không thêm markdown:
{
  ""patientName"": ""Họ tên bệnh nhân ghi trên toa, hoặc null nếu không đọc được"",
  ""doctorName"": ""Tên bác sĩ kê đơn (kèm chức danh nếu có), hoặc null"",
  ""hospital"": ""Tên bệnh viện/phòng khám nơi kê đơn, hoặc null"",
  ""diagnosis"": ""Chẩn đoán ghi trên toa (ví dụ: Viêm họng cấp), hoặc null"",
  ""medications"": [""Tên thuốc và liều dùng dạng chuỗi văn bản, ví dụ: 'ACEMUC 100mg gói - sáng 1 gói - chiều 1 gói - 06 gói'""]
}
Nếu ảnh không phải toa thuốc hoặc không đọc được, trả về medications là mảng rỗng.";

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
                    _logger.LogWarning("PrescriptionOcr Gemini error: {Body}", truncated);
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

                var result = new PrescriptionOcrRawResult
                {
                    PatientName = GetOptionalString(aiRoot, "patientName"),
                    DoctorName = GetOptionalString(aiRoot, "doctorName"),
                    Hospital = GetOptionalString(aiRoot, "hospital"),
                    Diagnosis = GetOptionalString(aiRoot, "diagnosis")
                };

                if (aiRoot.TryGetProperty("medications", out var meds) && meds.ValueKind == JsonValueKind.Array)
                {
                    result.MedicationLines = meds.EnumerateArray()
                        .Select(m => m.ValueKind == JsonValueKind.String ? m.GetString() ?? "" : "")
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PrescriptionOcr Gemini call failed");
                return null;
            }
        }

        private static string? GetOptionalString(JsonElement root, string prop)
        {
            if (!root.TryGetProperty(prop, out var value) || value.ValueKind != JsonValueKind.String) return null;
            var s = value.GetString();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
    }
}
