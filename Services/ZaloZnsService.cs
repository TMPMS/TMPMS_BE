using Services.Interfaces;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TMPMS.Services
{
    // Gửi OTP qua Zalo ZNS (Zalo Notification Service) dùng Zalo Official Account Open API.
    // Cần: đăng ký Zalo OA doanh nghiệp trên oa.zalo.me + duyệt template loại "OTP" trên
    // business.zalo.me, rồi điền ZaloZNS:AccessToken + ZaloZNS:TemplateId vào appsettings.json.
    // Access token OA hết hạn theo thời gian (thường vài giờ) — cần vào OA/App dev để làm mới
    // thủ công cho tới khi có cơ chế tự refresh bằng refresh_token.
    public class ZaloZnsService : ISmsService
    {
        private const string SendUrl = "https://business.openapi.zalo.me/message/template";

        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ZaloZnsService> _logger;

        public ZaloZnsService(IConfiguration configuration, ILogger<ZaloZnsService> logger)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
            _logger = logger;
        }

        public async Task<bool> SendSmsAsync(string toPhoneNumber, string message)
        {
            var accessToken = _configuration["ZaloZNS:AccessToken"];
            var templateId = _configuration["ZaloZNS:TemplateId"];

            // Chưa cấu hình Zalo OA (chưa đăng ký/duyệt template) → mock ra log để dev/QA vẫn chạy được flow.
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(templateId))
            {
                _logger.LogInformation("[MOCK Zalo ZNS OTP] Gửi tới SĐT: {Phone} | Nội dung: {Message}", toPhoneNumber, message);
                return true;
            }

            try
            {
                var formattedPhone = NormalizeVietnamPhone(toPhoneNumber);
                var otpCode = ExtractOtpCode(message);

                var payload = new
                {
                    phone = formattedPhone,
                    template_id = templateId,
                    template_data = new { otp = otpCode }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, SendUrl);
                request.Headers.Add("access_token", accessToken);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(body);
                var errorCode = doc.RootElement.TryGetProperty("error", out var errProp) ? errProp.GetInt32() : -1;

                if (response.IsSuccessStatusCode && errorCode == 0)
                {
                    _logger.LogInformation("Zalo ZNS OTP sent successfully to {Phone}", formattedPhone);
                    return true;
                }

                _logger.LogWarning("Zalo ZNS OTP error response: {Body}", body);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Zalo ZNS OTP call failed");
                return false;
            }
        }

        // Zalo ZNS yêu cầu số điện thoại dạng 84xxxxxxxxx, không có dấu +.
        private static string NormalizeVietnamPhone(string phone)
        {
            var trimmed = phone.Trim();
            if (trimmed.StartsWith("+84")) return trimmed.Substring(1);
            if (trimmed.StartsWith("0")) return "84" + trimmed.Substring(1);
            return trimmed;
        }

        // AuthService.SendOtp truyền message dạng text tự do ("...Ma OTP cua ban la: 123456...");
        // trích mã 6 số ra để đưa vào template_data thay vì đổi cả ISmsService interface.
        private static string ExtractOtpCode(string message)
        {
            var match = Regex.Match(message, @"\d{6}");
            return match.Success ? match.Value : message;
        }
    }
}
