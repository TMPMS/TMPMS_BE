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
                    // 1. Fetch available medicines from the database to inject as LLM context
                    var medicines = await _context.Medicines
                        .Select(m => new { m.Id, m.Name, m.Description })
                        .ToListAsync();

                    var medicinesContext = string.Join("\n", medicines.Select(m => $"- ID: {m.Id}, Tên: {m.Name}, Mô tả: {m.Description}"));

                    // 2. Build structured prompt instructing Gemini to output schema-compliant JSON
                    string prompt = $@"Bạn là trợ lý tư vấn y học cổ truyền (Đông Y) thân thiện của hệ thống nhà thuốc TMPMS.
Hãy phân tích triệu chứng của bệnh nhân và đưa ra lời khuyên ngắn gọn bằng tiếng Việt (tối đa 2-3 câu).
Sau đó, hãy đề xuất đúng một sản phẩm phù hợp nhất từ danh sách sản phẩm hiện có trong kho thuốc của chúng tôi dưới đây.

Danh sách sản phẩm thuốc/dược liệu hiện có:
{medicinesContext}

Yêu cầu định dạng kết quả trả về bắt buộc phải là JSON hợp lệ theo cấu trúc sau:
{{
  ""reply"": ""Lời khuyên tư vấn của bạn bằng tiếng Việt..."",
  ""recommendedMedicineId"": 101 // ID của sản phẩm được khuyên dùng từ danh sách trên (là kiểu số), hoặc null nếu không có sản phẩm nào phù hợp
}}

Câu hỏi/triệu chứng của bệnh nhân:
""{request.Text}""";

                    // 3. Construct payload for Gemini API
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
                        $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}",
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

                                string reply = aiRoot.TryGetProperty("reply", out var replyProp)
                                    ? replyProp.GetString() ?? ""
                                    : "";

                                int? recommendedId = null;
                                if (aiRoot.TryGetProperty("recommendedMedicineId", out var idProp) && idProp.ValueKind == JsonValueKind.Number)
                                {
                                    recommendedId = idProp.GetInt32();
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
                                    text = reply,
                                    product = recommendedProduct
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Gemini Chatbot Error]: {ex.Message}. Falling back to keyword search.");
                }
            }

            // ==========================================
            // FALLBACK FLOW (Keyword Matching)
            // ==========================================
            var lowerText = request.Text.ToLower();
            string queryTerm = "";
            string replyText = "Tôi đã nhận được thông tin về triệu chứng của bạn. Để được tư vấn chính xác nhất, bạn có thể mô tả chi tiết hơn không? Hoặc bạn có thể tìm các thuốc liên quan đến \"đau khớp\", \"dạ dày\", \"mệt mỏi\", \"táo bón\".";

            string[] jointPainKeywords = { "khớp", "khop", "lưng", "lung", "vai gáy", "vai gay", "khương thảo đan", "khuong thao dan" };
            string[] stomachPainKeywords = { "dạ dày", "da day", "trào ngược", "trao nguoc", "bụng", "bung", "bình vị", "binh vi" };
            string[] fatigueKeywords = { "mệt mỏi", "met moi", "sâm", "sam", "yếu", "yeu", "sinh lực", "sinh luc" };
            string[] constipationKeywords = { "táo bón", "tao bon", "tiêu hóa", "tieu hoa", "phân cứng", "phan cung", "gokids", "nhuận tràng", "nhuan trang" };

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
                replyText = "Bé hoặc người lớn bị táo bón, khó đi ngoài nên bổ sung Cốm Nhuận Tràng Gokids giúp làm mềm phân, kích thích nhu động ruột an toàn.";
                queryTerm = "Gokids";
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
                text = replyText,
                product = recommendedProductFallback
            });
        }
    }
}
