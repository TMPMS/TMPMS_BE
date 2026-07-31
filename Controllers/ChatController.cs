using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using TMPMS.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly TMPMSDbContext _context;
        private readonly IConfiguration _config;

        public ChatController(TMPMSDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public class ChatRequest
        {
            public string Text { get; set; } = "";
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Text))
            {
                return BadRequest(new { error = "Text is required" });
            }

            var apiKey = _config["Gemini:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                try
                {
                    // 1. Fetch available medicines from DB for LLM context
                    var medicines = await _context.Medicines
                        .Select(m => new { m.Id, m.Name, m.Description })
                        .ToListAsync();

                    var medicinesContext = string.Join("\n", medicines.Select(m => $"- ID: {m.Id}, Tên: {m.Name}, Mô tả: {m.Description}"));

                    // 2. Build structured prompt with Intent Classification
                    string prompt = $@"Bạn là trợ lý AI thông minh của hệ thống nhà thuốc/phòng khám y học cổ truyền TMPMS.
Nhiệm vụ của bạn là phân tích ý định của người dùng và trả về JSON theo đúng cấu trúc yêu cầu.

Danh sách sản phẩm hiện có trong kho thuốc:
{medicinesContext}

Quy tắc phân loại ý định (intent):
1. ""SYMPTOM_CONSULT"": Khách hàng mô tả triệu chứng bệnh hoặc xin tư vấn về thuốc/sức khỏe.
   - reply: Lời khuyên tư vấn ngắn gọn (2-3 câu).
   - recommendedMedicineId: ID sản phẩm phù hợp nhất từ danh sách trên (hoặc null nếu không có).
   - suggestedAction: {{ ""type"": ""none"", ""label"": """" }}

2. ""APPOINTMENT"": Khách hàng muốn đặt lịch khám bệnh, hẹn gặp bác sĩ/dược sĩ hoặc chọn giờ tư vấn.
   - reply: Xóa tan lo lắng, hướng dẫn khách bấm nút để chọn giờ khám.
   - recommendedMedicineId: null
   - suggestedAction: {{ ""type"": ""navigate_to_booking"", ""label"": ""📅 Đặt lịch khám ngay"" }}

3. ""LIVE_PHARMACIST"": Khách hàng muốn nói chuyện, trao đổi trực tiếp với Dược sĩ thật (người thật).
   - reply: Thông báo sẵn sàng kết nối ngay với Dược sĩ tư vấn chuyên môn.
   - recommendedMedicineId: null
   - suggestedAction: {{ ""type"": ""open_pharmacist_chat"", ""label"": ""💬 Nối máy với Dược sĩ thật"" }}

4. ""PRESCRIPTION_LOOKUP"": Khách hàng hỏi về đơn thuốc, phiếu chẩn đoán hoặc lịch sử khám bệnh của mình.
   - reply: Hướng dẫn khách hàng vào hồ sơ cá nhân để tra cứu toàn bộ đơn thuốc và lịch sử khám.
   - recommendedMedicineId: null
   - suggestedAction: {{ ""type"": ""navigate_to_history"", ""label"": ""📋 Xem lịch sử & Đơn thuốc"" }}

5. ""STORE_INFO"": Khách hàng hỏi về địa chỉ nhà thuốc, giờ mở cửa, số điện thoại hotline hoặc vị trí.
   - reply: Cung cấp thông tin chi tiết: Mở cửa 07:00 - 21:30 hàng ngày. Địa chỉ: 123 Đường Y Học Cổ Truyền, Q.1, TP.HCM. Hotline: 1900 6789.
   - recommendedMedicineId: null
   - suggestedAction: {{ ""type"": ""none"", ""label"": """" }}

6. ""GENERAL_CHAT"": Chào hỏi, cảm ơn hoặc các câu giao tiếp thông thường.
   - reply: Chào hỏi thân thiện, giới thiệu các tính năng (tư vấn sức khỏe, đặt lịch khám, nối máy Dược sĩ thật).
   - recommendedMedicineId: null
   - suggestedAction: {{ ""type"": ""none"", ""label"": """" }}

BẮT BUỘC định dạng đầu ra phải là JSON hợp lệ theo schema:
{{
  ""intent"": ""SYMPTOM_CONSULT | APPOINTMENT | LIVE_PHARMACIST | PRESCRIPTION_LOOKUP | STORE_INFO | GENERAL_CHAT"",
  ""reply"": ""Nội dung trả lời..."",
  ""recommendedMedicineId"": 101,
  ""suggestedAction"": {{
    ""type"": ""navigate_to_booking | open_pharmacist_chat | navigate_to_history | none"",
    ""label"": ""Nút gợi ý...""
  }}
}}

Câu hỏi của người dùng:
""{request.Text}""";

                    var payload = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new[]
                                {
                                    new { text = prompt }
                                }
                            }
                        },
                        generationConfig = new
                        {
                            responseMimeType = "application/json"
                        }
                    };

                    using var client = new HttpClient();
                    var response = await client.PostAsync(
                        $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-lite:generateContent?key={apiKey}",
                        new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(responseString);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("candidates", out var candidates) &&
                            candidates.GetArrayLength() > 0 &&
                            candidates[0].TryGetProperty("content", out var content) &&
                            content.TryGetProperty("parts", out var parts) &&
                            parts.GetArrayLength() > 0 &&
                            parts[0].TryGetProperty("text", out var textProp))
                        {
                            var aiJsonText = textProp.GetString();
                            if (!string.IsNullOrEmpty(aiJsonText))
                            {
                                using var aiDoc = JsonDocument.Parse(aiJsonText);
                                var aiRoot = aiDoc.RootElement;

                                string intent = aiRoot.TryGetProperty("intent", out var intentProp)
                                    ? intentProp.GetString() ?? "GENERAL_CHAT"
                                    : "GENERAL_CHAT";

                                string reply = aiRoot.TryGetProperty("reply", out var replyProp)
                                    ? replyProp.GetString() ?? ""
                                    : "";

                                int? recommendedId = null;
                                if (aiRoot.TryGetProperty("recommendedMedicineId", out var idProp) && idProp.ValueKind == JsonValueKind.Number)
                                {
                                    recommendedId = idProp.GetInt32();
                                }

                                string actionType = "none";
                                string actionLabel = "";
                                if (aiRoot.TryGetProperty("suggestedAction", out var actObj) && actObj.ValueKind == JsonValueKind.Object)
                                {
                                    if (actObj.TryGetProperty("type", out var typeProp)) actionType = typeProp.GetString() ?? "none";
                                    if (actObj.TryGetProperty("label", out var labelProp)) actionLabel = labelProp.GetString() ?? "";
                                }

                                object? recommendedProduct = null;
                                if (recommendedId.HasValue)
                                {
                                    var medicine = await _context.Medicines
                                        .FirstOrDefaultAsync(m => m.Id == recommendedId.Value);
                                    if (medicine != null)
                                    {
                                        recommendedProduct = new
                                        {
                                            id = medicine.Id,
                                            name = medicine.Name,
                                            price = medicine.Price,
                                            image = medicine.ImageUrl ?? "https://images.unsplash.com/photo-1615485290382-441e4d049cb5?w=400&h=400&fit=crop",
                                            unit = medicine.Unit ?? "Hộp"
                                        };
                                    }
                                }

                                return Ok(new
                                {
                                    intent = intent,
                                    text = reply,
                                    product = recommendedProduct,
                                    suggestedAction = new
                                    {
                                        type = actionType,
                                        label = actionLabel
                                    }
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Gemini Chatbot Error]: {ex.Message}. Falling back to rule-based keyword matching.");
                }
            }

            // =========================================================================
            // BƯỚC 3 — UPGRADED FALLBACK FLOW (Rule-Based Intent Matching for Keyword Search)
            // =========================================================================
            var lowerText = request.Text.ToLower().Trim();

            // 1. Intent: APPOINTMENT
            string[] appointmentKeywords = { "đặt lịch", "hẹn khám", "gặp bác sĩ", "khám bệnh", "lịch hẹn", "đặt hẹn", "đặt lịch hẹn", "booking" };
            if (appointmentKeywords.Any(k => lowerText.Contains(k)))
            {
                return Ok(new
                {
                    intent = "APPOINTMENT",
                    text = "Hệ thống hỗ trợ đặt lịch hẹn khám bệnh trực tiếp với Bác sĩ Đông Y chuyên khoa. Bạn có thể nhấn vào nút bên dưới để chọn giờ khám ngay.",
                    product = (object?)null,
                    suggestedAction = new { type = "navigate_to_booking", label = "📅 Đặt lịch khám ngay" }
                });
            }

            // 2. Intent: LIVE_PHARMACIST
            string[] pharmacistKeywords = { "dược sĩ", "tư vấn trực tiếp", "nói chuyện với người thật", "người thật", "gặp dược sĩ", "dược sĩ thật", "tư vấn viên" };
            if (pharmacistKeywords.Any(k => lowerText.Contains(k)))
            {
                return Ok(new
                {
                    intent = "LIVE_PHARMACIST",
                    text = "Bạn muốn nhắn tin trực tiếp với Dược sĩ tư vấn chuyên môn của nhà thuốc? Vui lòng nhấn nút bên dưới để nối máy với Dược sĩ thật ngay lập tức.",
                    product = (object?)null,
                    suggestedAction = new { type = "open_pharmacist_chat", label = "💬 Nối máy với Dược sĩ thật" }
                });
            }

            // 3. Intent: PRESCRIPTION_LOOKUP
            string[] prescriptionKeywords = { "đơn thuốc", "lịch sử khám", "lịch sử", "phiếu khám", "bệnh án", "xem đơn", "đã mua" };
            if (prescriptionKeywords.Any(k => lowerText.Contains(k)))
            {
                return Ok(new
                {
                    intent = "PRESCRIPTION_LOOKUP",
                    text = "Bạn có thể xem lại toàn bộ lịch sử khám bệnh và các đơn thuốc đã được kê trong hồ sơ bệnh nhân của mình.",
                    product = (object?)null,
                    suggestedAction = new { type = "navigate_to_history", label = "📋 Xem lịch sử & Đơn thuốc" }
                });
            }

            // 4. Intent: STORE_INFO
            string[] storeInfoKeywords = { "địa chỉ", "giờ mở cửa", "ở đâu", "số điện thoại", "hotline", "liên hệ", "vị trí", "địa điểm" };
            if (storeInfoKeywords.Any(k => lowerText.Contains(k)))
            {
                return Ok(new
                {
                    intent = "STORE_INFO",
                    text = "Nhà thuốc TMPMS phục vụ từ 07:00 đến 21:30 tất cả các ngày trong tuần (kể cả Lễ, Tết).\n• Địa chỉ: 123 Đường Y Học Cổ Truyền, Q.1, TP.HCM.\n• Hotline tư vấn: 1900 6789.",
                    product = (object?)null,
                    suggestedAction = new { type = "none", label = "" }
                });
            }

            // 5. Intent: GENERAL_CHAT (Greetings)
            string[] greetingKeywords = { "chào", "xin chào", "hello", "hi", "chào bot", "ơi", "cảm ơn", "thank" };
            if (greetingKeywords.Any(k => lowerText == k || lowerText.StartsWith(k + " ") || lowerText.EndsWith(" " + k)))
            {
                return Ok(new
                {
                    intent = "GENERAL_CHAT",
                    text = "Xin chào! Tôi là Trợ lý Dược sĩ AI của TMPMS. Tôi có thể hỗ trợ bạn tư vấn triệu chứng bệnh, hướng dẫn đặt lịch khám hoặc kết nối với Dược sĩ thật. Bạn cần tôi giúp gì hôm nay?",
                    product = (object?)null,
                    suggestedAction = new { type = "none", label = "" }
                });
            }

            // 6. Intent: SYMPTOM_CONSULT
            string queryTerm = "";
            string replyText = "Tôi đã ghi nhận thông tin sức khỏe của bạn. Để tư vấn chính xác nhất, bạn hãy mô tả chi tiết triệu chứng hoặc tìm các từ khóa như \"đau khớp\", \"dạ dày\", \"mệt mỏi\", \"táo bón\".";

            string[] jointPainKeywords = { "khớp", "khop", "lưng", "lung", "vai gáy", "khương thảo đan" };
            string[] stomachPainKeywords = { "dạ dày", "da day", "trào ngược", "bụng", "bình vị" };
            string[] fatigueKeywords = { "mệt mỏi", "met moi", "sâm", "yếu", "sinh lực" };
            string[] constipationKeywords = { "táo bón", "tao bon", "tiêu hóa", "gokids", "nhuận tràng" };

            string matchedIntent = "SYMPTOM_CONSULT";

            if (jointPainKeywords.Any(k => lowerText.Contains(k)))
            {
                replyText = "Đối với các triệu chứng đau nhức xương khớp, thoái hóa khớp, tôi khuyên dùng viên uống Khương Thảo Đan giúp giảm đau xương khớp, tái tạo sụn khớp hiệu quả.";
                queryTerm = "Khương Thảo Đan";
            }
            else if (stomachPainKeywords.Any(k => lowerText.Contains(k)))
            {
                replyText = "Triệu chứng trào ngược dạ dày, viêm loét dạ dày có thể được hỗ trợ cải thiện rất tốt nhờ Bình Vị giúp giảm tiết acid, bảo vệ niêm mạc dạ dày.";
                queryTerm = "Bình Vị";
            }
            else if (fatigueKeywords.Any(k => lowerText.Contains(k)))
            {
                replyText = "Để bồi bổ sức khỏe, tăng cường sinh lực và tăng sức đề kháng chống mệt mỏi, Trà Sâm là sự lựa chọn tuyệt vời.";
                queryTerm = "Sâm";
            }
            else if (constipationKeywords.Any(k => lowerText.Contains(k)))
            {
                replyText = "Bị táo bón, khó đi ngoài nên bổ sung Cốm Nhuận Tràng Gokids giúp làm mềm phân, kích thích nhu động ruột an toàn.";
                queryTerm = "Gokids";
            }
            else
            {
                matchedIntent = "GENERAL_CHAT";
            }

            object? recommendedProductFallback = null;
            if (!string.IsNullOrEmpty(queryTerm))
            {
                var medicine = await _context.Medicines
                    .FirstOrDefaultAsync(m => m.Name.Contains(queryTerm) || m.Description.Contains(queryTerm));

                if (medicine != null)
                {
                    recommendedProductFallback = new
                    {
                        id = medicine.Id,
                        name = medicine.Name,
                        price = medicine.Price,
                        image = medicine.ImageUrl ?? "https://images.unsplash.com/photo-1615485290382-441e4d049cb5?w=400&h=400&fit=crop",
                        unit = medicine.Unit ?? "Hộp"
                    };
                }
            }

            return Ok(new
            {
                intent = matchedIntent,
                text = replyText,
                product = recommendedProductFallback,
                suggestedAction = new { type = "none", label = "" }
            });
        }
    }
}
