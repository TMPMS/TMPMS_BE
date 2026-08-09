using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace TMPMS.Services
{
    // Dùng Gemini Vision để đọc tên sản phẩm in trên vỏ/hộp thuốc do khách hàng chụp/dán ảnh ở
    // ô tìm kiếm bằng hình ảnh (giống cơ chế đọc toa thuốc ở PrescriptionOcrService), rồi trả về
    // dạng từ khoá văn bản để tái sử dụng luôn pipeline tìm kiếm theo tên đã có sẵn.
    public class MedicineImageSearchService : IMedicineImageSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MedicineImageSearchService> _logger;

        private static readonly string[] ModelsToTry = { "gemini-2.5-flash", "gemini-3.5-flash-lite", "gemini-2.0-flash", "gemini-flash-latest" };

        public MedicineImageSearchService(HttpClient httpClient, IConfiguration configuration, ILogger<MedicineImageSearchService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string?> IdentifyAsync(byte[] imageBytes, string mimeType)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || imageBytes.Length == 0) return null;

            try
            {
                const string prompt = @"Bạn là trợ lý của một nhà thuốc. Nhìn ảnh vỏ hộp/chai/lọ thuốc hoặc thực phẩm chức năng
này và đọc TÊN SẢN PHẨM in trên bao bì (thương hiệu + tên gọi, bỏ qua hàm lượng/quy cách/số lô).
Trả về CHỈ một JSON theo đúng schema sau, không thêm giải thích, không thêm markdown:
{
  ""productName"": ""Tên sản phẩm ngắn gọn dùng để tìm kiếm, hoặc null nếu ảnh không phải bao bì thuốc/thực phẩm chức năng hoặc không đọc được""
}";

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
                    _logger.LogWarning("MedicineImageSearch Gemini error: {Body}", truncated);
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
                if (!aiRoot.TryGetProperty("productName", out var nameProp) || nameProp.ValueKind != JsonValueKind.String)
                    return null;

                var productName = nameProp.GetString();
                return string.IsNullOrWhiteSpace(productName) ? null : productName.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MedicineImageSearch Gemini call failed");
                return null;
            }
        }
    }
}
