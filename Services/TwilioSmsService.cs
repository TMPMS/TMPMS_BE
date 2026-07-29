using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace TMPMS.Services
{
    public class TwilioSmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public TwilioSmsService(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        public async Task<bool> SendSmsAsync(string toPhoneNumber, string message)
        {
            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];
            var fromNumber = _configuration["Twilio:FromNumber"];

            // If Twilio is not configured, write mock to console and return true for local development
            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken))
            {
                Console.WriteLine($"\n[MOCK SMS] Gửi tới SĐT: {toPhoneNumber}");
                Console.WriteLine($"[MOCK SMS] Nội dung: {message}\n");
                return true;
            }

            try
            {
                // Normalize Vietnam phone numbers to international format (+84...)
                var formattedPhone = toPhoneNumber.Trim();
                if (formattedPhone.StartsWith("0"))
                {
                    formattedPhone = "+84" + formattedPhone.Substring(1);
                }

                var requestUrl = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("To", formattedPhone),
                    new KeyValuePair<string, string>("From", fromNumber),
                    new KeyValuePair<string, string>("Body", message)
                });
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Twilio SMS] Gửi tin nhắn thực tế thành công tới {formattedPhone}");
                    return true;
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[Twilio SMS Error] Phản hồi lỗi: {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Twilio SMS Exception] Lỗi ngoại lệ: {ex.Message}");
                return false;
            }
        }
    }
}
