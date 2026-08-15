using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using TMPMS.Data;
using TMPMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly TMPMSDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<ChatController> _logger;
        private const decimal DefaultAppointmentDeposit = 100000m; // Khớp AppointmentController.DefaultDeposit

        public ChatController(TMPMSDbContext context, IConfiguration config, ILogger<ChatController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : (int?)null;
        }

        public class ChatMessageItem
        {
            public string Role { get; set; } = "user"; // "user" or "model"
            public string Text { get; set; } = "";
        }

        public class ChatRequest
        {
            public string Text { get; set; } = "";
            public List<ChatMessageItem>? History { get; set; }
            // Ảnh khách đính kèm (vd chụp/dán ảnh vỏ hộp thuốc) — base64 thuần, không kèm tiền tố "data:...;base64,".
            public string? ImageBase64 { get; set; }
            public string? ImageMimeType { get; set; }
        }

        private const int MaxImageBase64Length = 7_000_000; // ~5MB ảnh gốc sau khi encode base64

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Text) && string.IsNullOrEmpty(request.ImageBase64))
            {
                return BadRequest(new { error = "Text is required" });
            }

            string? imageBase64 = request.ImageBase64;
            if (!string.IsNullOrEmpty(imageBase64))
            {
                var commaIdx = imageBase64.IndexOf(',');
                if (imageBase64.StartsWith("data:", StringComparison.Ordinal) && commaIdx > 0)
                    imageBase64 = imageBase64.Substring(commaIdx + 1);
                if (imageBase64.Length > MaxImageBase64Length)
                    return BadRequest(new { error = "Ảnh quá lớn, vui lòng chọn ảnh dưới 5MB" });
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

                    // 1b. Fetch REAL available appointment slots from DB (3 ngày tới) để AI không tự bịa giờ trống.
                    var slotsContext = await BuildAvailableSlotsContextAsync();

                    // 2. Build structured prompt with Intent Classification & Multi-turn memory
                    var nowLocal = DateTime.Now;
                    string systemPrompt = $@"Bạn là trợ lý AI thông minh của hệ thống nhà thuốc/phòng khám y học cổ truyền TMPMS.
Nhiệm vụ của bạn là phân tích ý định của người dùng (dựa trên câu hỏi HIỆN TẠI và LỊCH SỬ HỘI THOẠI trước đó) và trả về JSON theo đúng cấu trúc yêu cầu.

QUY TẮC BẮT BUỘC (áp dụng cho mọi intent): mọi thông tin bạn trích xuất (triệu chứng, tên sản phẩm, thời gian...) phải dựa trên những gì khách THỰC SỰ đã nói — ở tin nhắn hiện tại HOẶC bất kỳ tin nhắn nào trước đó trong lịch sử hội thoại. TUYỆT ĐỐI KHÔNG được tự bịa đặt/suy đoán thông tin khách chưa từng cung cấp. Nếu thiếu thông tin cần thiết để hoàn tất một hành động, hãy hỏi lại khách trong phần reply thay vì tự giả định.

QUY TẮC KHI TIN NHẮN HIỆN TẠI CÓ KÈM HÌNH ẢNH: nếu khách gửi kèm 1 ảnh (vd ảnh chụp vỏ hộp thuốc, ảnh sản phẩm), hãy quan sát kỹ ảnh và cố nhận diện tên thuốc/sản phẩm xuất hiện trong ảnh, rồi so khớp với ""Danh sách sản phẩm hiện có"" bên dưới. Nếu khớp được đúng 1 sản phẩm, coi như khách đang hỏi về sản phẩm đó (chọn intent phù hợp theo yêu cầu kèm theo — vd không nói gì thêm thì dùng SYMPTOM_CONSULT để giới thiệu sản phẩm, nói ""mua""/""thêm vào giỏ"" thì dùng ORDER_MEDICINE) và điền recommendedMedicineId tương ứng. Nếu ảnh mờ, không phải ảnh thuốc, hoặc không khớp được sản phẩm nào trong danh sách, dùng intent GENERAL_CHAT và trả lời trung thực rằng bạn không chắc chắn nhận diện được sản phẩm trong ảnh, đề nghị khách mô tả thêm bằng lời hoặc chụp ảnh rõ hơn — không được bịa tên sản phẩm không có trong danh sách.

Thời điểm hiện tại (giờ Việt Nam, dùng để quy đổi ""hôm nay/mai/chiều nay""...): {nowLocal:dddd, dd/MM/yyyy HH:mm} ({nowLocal:yyyy-MM-ddTHH:mm:ss})

Danh sách sản phẩm hiện có trong kho thuốc:
{medicinesContext}

Khung giờ khám THỰC TẾ còn trống (dữ liệu truy vấn trực tiếp từ hệ thống đặt lịch, tính đến thời điểm hiện tại — đây là nguồn DUY NHẤT bạn được phép dùng khi nhắc tới giờ trống):
{slotsContext}

QUY TẮC VỀ GIỜ TRỐNG: khi khách hỏi còn giờ nào trống hoặc bạn cần gợi ý giờ khám cụ thể, CHỈ được liệt kê những giờ có trong danh sách ""Khung giờ khám THỰC TẾ còn trống"" ở trên. TUYỆT ĐỐI KHÔNG được tự bịa/đoán ra bất kỳ khung giờ nào không có trong danh sách đó. Nếu danh sách trống hoặc không đủ dữ liệu, hãy nói thật là chưa xác định được giờ trống và hướng dẫn khách bấm nút đặt lịch để xem đầy đủ, không được tự nghĩ ra giờ.

Quy tắc phân loại ý định (intent):
1. ""SYMPTOM_CONSULT"": Khách hàng mô tả triệu chứng bệnh, xin tư vấn về thuốc/sức khỏe hoặc hỏi câu follow-up đề xuất thuốc dựa trên triệu chứng đã nói ở các tin nhắn trước.
   - reply: Lời khuyên tư vấn ngắn gọn (2-3 câu), giải thích lý do đề xuất thuốc liên quan đến triệu chứng.
   - recommendedMedicineId: ID sản phẩm phù hợp nhất từ danh sách trên (hoặc null nếu không có).
   - suggestedAction: {{ ""type"": ""none"", ""label"": """" }}

2. ""APPOINTMENT"": Khách hàng muốn đặt lịch khám bệnh, hẹn gặp bác sĩ/dược sĩ hoặc chọn giờ tư vấn, nhưng CHƯA nêu rõ một thời điểm cụ thể (nếu đã nêu rõ ngày+giờ thì dùng intent ""BOOK_APPOINTMENT_NOW"" bên dưới).
   - reply: Xóa tan lo lắng, gợi ý một vài khung giờ trống LẤY ĐÚNG TỪ danh sách ""Khung giờ khám THỰC TẾ còn trống"" ở trên (ví dụ 2-4 khung giờ gần nhất), rồi hướng dẫn khách bấm nút bên dưới nếu muốn xem đầy đủ hoặc chọn giờ khác. TUYỆT ĐỐI KHÔNG được nêu bất kỳ giờ nào không có trong danh sách đó — nếu danh sách rỗng thì chỉ hướng dẫn khách bấm nút, không đoán giờ.
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

7. ""ORDER_MEDICINE"": Khách hàng ra lệnh MUA hoặc THÊM VÀO GIỎ một sản phẩm cụ thể, có nêu rõ tên sản phẩm (ví dụ: ""mua 2 hộp sâm nhật"", ""thêm 1 chai mật ong vào giỏ""). Chỉ dùng intent này khi xác định được sản phẩm khớp trong danh sách trên.
   - reply: Xác nhận ngắn gọn đã thêm sản phẩm nào, số lượng bao nhiêu vào giỏ hàng.
   - recommendedMedicineId: ID sản phẩm khớp nhất trong danh sách (bắt buộc phải có, không được null).
   - quantity: số lượng khách yêu cầu (số nguyên dương, mặc định 1 nếu khách không nói rõ số lượng).
   - wantsCheckout: true nếu khách dùng động từ ngụ ý mua ngay/thanh toán ngay (""mua"", ""đặt mua"", ""thanh toán luôn"", ""mua luôn""); false nếu khách chỉ nói ""thêm vào giỏ""/""bỏ vào giỏ"" (chưa muốn thanh toán ngay).
   - suggestedAction: {{ ""type"": ""none"", ""label"": """" }}

8. ""REMOVE_FROM_CART"": Khách hàng muốn GỠ, XÓA hoặc BỚT một sản phẩm cụ thể đang có trong giỏ hàng (ví dụ: ""gỡ 5 chai mật ong ở giỏ hàng đi"", ""xóa mật ong khỏi giỏ"", ""bớt 2 hộp sâm"", ""xóa hết sâm trong giỏ""). Chỉ dùng intent này khi xác định được sản phẩm khớp trong danh sách trên VÀ khách rõ ràng muốn LOẠI BỎ khỏi giỏ (không phải thêm/mua — đó là intent ORDER_MEDICINE).
   - reply: Xác nhận ngắn gọn sẽ gỡ sản phẩm nào khỏi giỏ hàng (backend sẽ tự kiểm tra giỏ hàng thực tế và ghi đè lại reply theo đúng số lượng thực sự đã gỡ, nên chỉ cần xác nhận ý định).
   - recommendedMedicineId: ID sản phẩm khớp nhất trong danh sách (bắt buộc phải có, không được null).
   - quantity: số lượng khách muốn gỡ bớt (số nguyên dương) — CHỈ điền khi khách nói rõ một số lượng cụ thể muốn gỡ bớt (không phải xóa hết).
   - removeAll: true nếu khách muốn xóa hẳn/xóa hết/toàn bộ sản phẩm này khỏi giỏ hoặc không nói rõ số lượng cần gỡ; false nếu khách nói rõ số lượng cụ thể muốn gỡ bớt (dùng quantity ở trên).
   - suggestedAction: {{ ""type"": ""none"", ""label"": """" }}

9. ""BOOK_APPOINTMENT_NOW"": Khách hàng muốn đặt lịch khám vào một THỜI ĐIỂM CỤ THỂ đã nêu rõ ngày/giờ (ví dụ: ""đặt lịch lúc 9h mai"", ""khám lúc 2h chiều thứ 6 tuần này"", ""đặt lịch 10h hôm nay""). Khác với intent ""APPOINTMENT"" (dùng khi khách chỉ nói muốn đặt lịch chung chung, CHƯA nêu giờ cụ thể) — nếu khách đã nói rõ thời điểm thì PHẢI dùng intent này để hệ thống tự giữ chỗ ngay lập tức.
   - resolvedDateTimeIso: QUY TẮC TÍNH TOÁN — chỉ dùng ĐÚNG 1 giá trị ""Thời điểm hiện tại"" đã cho ở trên làm mốc gốc (KHÔNG dùng bất kỳ ngày/giờ nào được nhắc trong lịch sử hội thoại làm mốc, dù trước đó có nói tới ngày khác). Quy đổi tuyệt đối theo định dạng ""yyyy-MM-ddTHH:mm:ss"". Giờ làm việc 08:00–17:30, chỉ nhận phút :00 hoặc :30 — nếu khách nói giờ lẻ (vd 9h15) thì làm tròn xuống mốc 30 phút gần nhất.
     QUAN TRỌNG: LUÔN tính ra resolvedDateTimeIso nếu câu nói đủ rõ để suy luận ra 1 ngày-giờ cụ thể — kể cả khi bạn nghĩ thời điểm đó đã qua hoặc không hợp lệ. ĐỪNG tự phán đoán/từ chối trong phần reply rằng thời điểm đó ""đã qua"" hay ""không hợp lệ"" — hệ thống backend sẽ tự kiểm tra chính xác bằng mã nguồn (so sánh đầy đủ cả ngày lẫn giờ, không chỉ giờ trong ngày) và tự trả lời khách nếu thực sự không hợp lệ. Việc của bạn CHỈ là tính đúng resolvedDateTimeIso theo lời khách nói, không phán xét tính hợp lệ. Chỉ để null nếu câu nói THỰC SỰ không đủ thông tin để suy luận ra ngày/giờ (khi đó dùng intent ""APPOINTMENT"" thay thế).
   - reply: Xác nhận đã giữ khung giờ (giả định resolvedDateTimeIso sẽ được giữ thành công — backend sẽ ghi đè reply nếu thất bại). NẾU đã biết triệu chứng (xem symptomHint bên dưới) thì nhắc lại triệu chứng đó trong câu trả lời để khách xác nhận là đúng ý. NẾU chưa biết triệu chứng, PHẢI hỏi lại khách khám vì lý do/triệu chứng gì (không được im lặng bỏ qua, không được tự bịa ra 1 triệu chứng khách chưa từng nói).
   - recommendedMedicineId: null
   - symptomHint: triệu chứng/lý do khám của khách. BẮT BUỘC phải tìm trong TOÀN BỘ cuộc hội thoại — kể cả những tin nhắn TRƯỚC ĐÓ trong lịch sử, không chỉ câu hiện tại (ví dụ: khách nói ""tôi bị đau đầu"" ở tin nhắn trước, rồi tin nhắn sau chỉ nói ""đặt lịch lúc 10h mai"" — vẫn phải lấy symptomHint = ""đau đầu"" từ tin nhắn trước). CHỈ được để null nếu thực sự không có triệu chứng nào được nhắc tới ở bất kỳ đâu trong hội thoại — TUYỆT ĐỐI KHÔNG được tự bịa/suy đoán một triệu chứng mà khách chưa từng nói.
   - suggestedAction: {{ ""type"": ""none"", ""label"": """" }}

BẮT BUỘC định dạng đầu ra phải là JSON hợp lệ theo schema:
{{
  ""intent"": ""SYMPTOM_CONSULT | APPOINTMENT | LIVE_PHARMACIST | PRESCRIPTION_LOOKUP | STORE_INFO | GENERAL_CHAT | ORDER_MEDICINE | REMOVE_FROM_CART | BOOK_APPOINTMENT_NOW"",
  ""reply"": ""Nội dung trả lời..."",
  ""recommendedMedicineId"": 101,
  ""quantity"": 1,
  ""wantsCheckout"": false,
  ""removeAll"": false,
  ""resolvedDateTimeIso"": null,
  ""symptomHint"": null,
  ""suggestedAction"": {{
    ""type"": ""navigate_to_booking | open_pharmacist_chat | navigate_to_history | none"",
    ""label"": ""Nút gợi ý...""
  }}
}}";

                    var contentsList = new List<object>();

                    // Multi-turn context injection
                    if (request.History != null && request.History.Count > 0)
                    {
                        foreach (var h in request.History)
                        {
                            if (string.IsNullOrWhiteSpace(h.Text)) continue;
                            string role = (h.Role == "model" || h.Role == "bot") ? "model" : "user";
                            contentsList.Add(new
                            {
                                role = role,
                                parts = new[] { new { text = h.Text } }
                            });
                        }
                    }

                    // Add current user request (kèm ảnh nếu có)
                    var currentParts = new List<object>
                    {
                        new { text = string.IsNullOrWhiteSpace(request.Text) ? "(Khách gửi kèm một hình ảnh, không có chú thích bằng lời)" : request.Text }
                    };
                    if (!string.IsNullOrEmpty(imageBase64))
                    {
                        currentParts.Add(new
                        {
                            inlineData = new
                            {
                                mimeType = string.IsNullOrWhiteSpace(request.ImageMimeType) ? "image/jpeg" : request.ImageMimeType,
                                data = imageBase64
                            }
                        });
                    }
                    contentsList.Add(new
                    {
                        role = "user",
                        parts = currentParts
                    });

                    var payload = new
                    {
                        systemInstruction = new
                        {
                            parts = new[] { new { text = systemPrompt } }
                        },
                        contents = contentsList,
                        generationConfig = new
                        {
                            responseMimeType = "application/json"
                        }
                    };

                    using var client = new HttpClient();

                    // Thử lần lượt nhiều model — nếu 1 tên model không còn khả dụng/không hợp lệ thì vẫn
                    // còn cơ hội trả lời bằng AI thật thay vì luôn rơi xuống chatbot rule-based ở BƯỚC 4.
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

                    if (response == null)
                    {
                        _logger.LogWarning("Gemini Chatbot: no model responded, falling back to rule-based keyword matching");
                    }
                    else if (!response.IsSuccessStatusCode)
                    {
                        var statusCode = (int)response.StatusCode;
                        var errStatus = "";
                        try
                        {
                            using var errDoc = JsonDocument.Parse(responseString);
                            if (errDoc.RootElement.TryGetProperty("error", out var errObj) &&
                                errObj.TryGetProperty("status", out var statusProp))
                            {
                                errStatus = statusProp.GetString() ?? "";
                            }
                        }
                        catch { }

                        var truncatedBody = responseString.Length > 500 ? responseString.Substring(0, 500) + "..." : responseString;
                        _logger.LogWarning("Gemini Chatbot error: HTTP {StatusCode} ({Reason}) status={ErrStatus} body={Body}", statusCode, response.ReasonPhrase, errStatus, truncatedBody);

                        if (statusCode == 429 || errStatus == "RESOURCE_EXHAUSTED")
                        {
                            return Ok(new
                            {
                                intent = "GENERAL_CHAT",
                                text = "Dược sĩ AI đang quá tải, vui lòng thử lại sau ít phút hoặc bấm 'Tư vấn Dược sĩ' để nói chuyện với dược sĩ thật.",
                                product = (object?)null,
                                suggestedAction = new { type = "open_pharmacist_chat", label = "💬 Tư vấn Dược sĩ" }
                            });
                        }
                    }
                    else
                    {
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

                                int quantity = 1;
                                if (aiRoot.TryGetProperty("quantity", out var qtyProp) && qtyProp.ValueKind == JsonValueKind.Number && qtyProp.GetInt32() > 0)
                                {
                                    quantity = qtyProp.GetInt32();
                                }

                                bool wantsCheckout = aiRoot.TryGetProperty("wantsCheckout", out var checkoutProp) && checkoutProp.ValueKind == JsonValueKind.True;
                                bool removeAll = aiRoot.TryGetProperty("removeAll", out var removeAllProp) && removeAllProp.ValueKind == JsonValueKind.True;
                                // REMOVE_FROM_CART không có "quantity" nghĩa là gỡ hết — không được mặc định về 1 như ORDER_MEDICINE.
                                bool removeQuantitySpecified = aiRoot.TryGetProperty("quantity", out var removeQtyProp) && removeQtyProp.ValueKind == JsonValueKind.Number && removeQtyProp.GetInt32() > 0;

                                DateTime? resolvedAppointmentDate = null;
                                if (aiRoot.TryGetProperty("resolvedDateTimeIso", out var dtProp) && dtProp.ValueKind == JsonValueKind.String
                                    && DateTime.TryParse(dtProp.GetString(), null, System.Globalization.DateTimeStyles.None, out var parsedDt))
                                {
                                    resolvedAppointmentDate = parsedDt;
                                }

                                string? symptomHint = aiRoot.TryGetProperty("symptomHint", out var symProp) && symProp.ValueKind == JsonValueKind.String
                                    ? symProp.GetString() : null;

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

                                // ORDER_MEDICINE: sản phẩm đã xác định — server tự quyết định action (không tin
                                // suggestedAction do Gemini trả, vì FE cần đúng 1 trong 2 giá trị cố định dưới đây
                                // để biết có tự thêm vào giỏ hàng hay không).
                                if (intent == "ORDER_MEDICINE" && recommendedProduct != null)
                                {
                                    actionType = wantsCheckout ? "add_to_cart_checkout" : "add_to_cart";
                                    actionLabel = wantsCheckout ? "🛒 Xem giỏ hàng & Thanh toán" : "🛒 Xem giỏ hàng";
                                }

                                // REMOVE_FROM_CART: giao việc gỡ khỏi giỏ cho FE thực hiện (giỏ hàng khách vãng lai chỉ
                                // tồn tại ở localStorage phía FE, BE không thấy được) — FE sẽ tự đối chiếu số lượng
                                // thực tế trong giỏ rồi ghi đè lại "reply" cho khớp với những gì thực sự đã xảy ra.
                                if (intent == "REMOVE_FROM_CART" && recommendedProduct != null)
                                {
                                    actionType = "remove_from_cart";
                                    actionLabel = "🛒 Xem giỏ hàng";
                                    if (!removeQuantitySpecified) removeAll = true;
                                }

                                // BOOK_APPOINTMENT_NOW: giữ chỗ ngay (không cần đợi khách bấm nút) — việc giữ chỗ
                                // không tốn tiền/không cam kết (tự hết hạn sau 15 phút nếu không hoàn tất đặt cọc),
                                // nên an toàn để thực hiện ngay khi khách nêu rõ thời điểm, đúng như khách yêu cầu.
                                object? appointmentResult = null;
                                if (intent == "BOOK_APPOINTMENT_NOW" && resolvedAppointmentDate.HasValue)
                                {
                                    var currentUserId = GetCurrentUserId();
                                    if (currentUserId == null)
                                    {
                                        reply = "Bạn cần đăng nhập để đặt lịch khám. Vui lòng đăng nhập rồi thử lại nhé!";
                                        actionType = "require_login";
                                        actionLabel = "🔐 Đăng nhập";
                                    }
                                    else
                                    {
                                        var (holdOk, holdError, holdInfo) = await TryHoldAppointmentSlotAsync(currentUserId.Value, resolvedAppointmentDate.Value, symptomHint);
                                        if (holdOk)
                                        {
                                            appointmentResult = holdInfo;
                                            actionType = "navigate_to_booking_checkout";
                                            actionLabel = "✅ Xác nhận & Đặt cọc lịch hẹn";
                                        }
                                        else
                                        {
                                            reply = holdError ?? reply;
                                            actionType = "navigate_to_booking";
                                            actionLabel = "📅 Chọn giờ khám khác";
                                        }
                                    }
                                }

                                return Ok(new
                                {
                                    intent = intent,
                                    text = reply,
                                    product = recommendedProduct,
                                    quantity = quantity,
                                    removeAll = removeAll,
                                    appointment = appointmentResult,
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
                    _logger.LogError(ex, "Gemini Chatbot call failed, falling back to rule-based keyword matching");
                }
            }

            // =========================================================================
            // BƯỚC 4 — UPGRADED MULTI-TURN FALLBACK FLOW (Rule-Based Context-Aware Engine)
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

            // 6. Intent: SYMPTOM_CONSULT (Direct + Multi-turn Context Lookup)
            string queryTerm = "";
            string replyText = "";
            string matchedIntent = "SYMPTOM_CONSULT";

            string[] jointPainKeywords = { "khớp", "khop", "lưng", "lung", "vai gáy", "khương thảo đan" };
            string[] stomachPainKeywords = { "dạ dày", "da day", "trào ngược", "bụng", "bình vị" };
            string[] fatigueKeywords = { "mệt mỏi", "met moi", "sâm", "yếu", "sinh lực" };
            string[] constipationKeywords = { "táo bón", "tao bon", "tiêu hóa", "gokids", "nhuận tràng" };

            // Check current message first
            bool currentHasJoint = jointPainKeywords.Any(k => lowerText.Contains(k));
            bool currentHasStomach = stomachPainKeywords.Any(k => lowerText.Contains(k));
            bool currentHasFatigue = fatigueKeywords.Any(k => lowerText.Contains(k));
            bool currentHasConstipation = constipationKeywords.Any(k => lowerText.Contains(k));

            if (currentHasJoint)
            {
                replyText = "Đối với các triệu chứng đau nhức xương khớp, thoái hóa khớp, tôi khuyên dùng Cao Xương Khớp Bách Thảo Dược giúp giảm đau xương khớp, tái tạo sụn khớp hiệu quả.";
                queryTerm = "Khớp";
            }
            else if (currentHasStomach)
            {
                replyText = "Triệu chứng trào ngược dạ dày, viêm loét dạ dày có thể được hỗ trợ cải thiện rất tốt nhờ các bài thuốc Đông Y giúp giảm tiết acid, bảo vệ niêm mạc dạ dày.";
                queryTerm = "Dạ Dày";
            }
            else if (currentHasFatigue)
            {
                replyText = "Để bồi bổ sức khỏe, tăng cường sinh lực và tăng sức đề kháng chống mệt mỏi, sản phẩm bồi bổ Đông Y là sự lựa chọn tuyệt vời.";
                queryTerm = "Não";
            }
            else if (currentHasConstipation)
            {
                replyText = "Bị táo bón, khó đi ngoài nên bổ sung sản phẩm mát gan giải độc giúp làm mềm phân, kích thích nhu động ruột an toàn.";
                queryTerm = "Gan";
            }
            else
            {
                // Multi-turn context fallback lookup from previous user messages in History
                string previousUserContext = "";
                if (request.History != null)
                {
                    var userHistoryTexts = request.History
                        .Where(h => (h.Role == "user" || h.Role == "human") && !string.IsNullOrWhiteSpace(h.Text))
                        .Select(h => h.Text.ToLower().Trim());
                    previousUserContext = string.Join(" ", userHistoryTexts);
                }

                if (previousUserContext.Contains("khớp") || previousUserContext.Contains("khop") || previousUserContext.Contains("lưng") || previousUserContext.Contains("vai gáy"))
                {
                    replyText = "Dựa trên tình trạng đau khớp bạn đã chia sẻ trước đó, tôi đề xuất sử dụng Cao Xương Khớp Bách Thảo Dược giúp giảm đau nhức xương khớp, phục hồi và tái tạo sụn khớp hiệu quả.";
                    queryTerm = "Khớp";
                }
                else if (previousUserContext.Contains("dạ dày") || previousUserContext.Contains("da day") || previousUserContext.Contains("trào ngược"))
                {
                    replyText = "Dựa trên tình trạng dạ dày bạn đã chia sẻ trước đó, tôi đề xuất các sản phẩm hỗ trợ tiêu hóa giúp giảm bớt cơn đau dạ dày và trung hòa acid dịch vị.";
                    queryTerm = "Dạ Dày";
                }
                else if (previousUserContext.Contains("mệt mỏi") || previousUserContext.Contains("met moi") || previousUserContext.Contains("yếu"))
                {
                    replyText = "Dựa trên biểu hiện mệt mỏi bạn đã đề cập, tôi khuyên dùng Hoạt Huyết Dưỡng Não giúp bổ sung năng lượng và tăng cường thể lực.";
                    queryTerm = "Não";
                }
                else if (previousUserContext.Contains("táo bón") || previousUserContext.Contains("tao bon") || previousUserContext.Contains("tiêu hóa"))
                {
                    replyText = "Dựa trên tình trạng khó tiêu hóa bạn đã nhắc tới, sản phẩm trà Cà Gai Leo giải độc gan mát ruột là giải pháp thích hợp nhất.";
                    queryTerm = "Gan";
                }
                else
                {
                    matchedIntent = "GENERAL_CHAT";
                    replyText = "Tôi đã ghi nhận thông tin sức khỏe của bạn. Để tư vấn chính xác nhất, bạn hãy mô tả chi tiết triệu chứng hoặc tìm các từ khóa như \"đau khớp\", \"dạ dày\", \"mệt mỏi\", \"táo bón\".";
                }
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

        // Truy vấn khung giờ trống THẬT từ DB cho 3 ngày tới, để nhồi vào prompt cho AI — cố ý mirror
        // logic của AppointmentController.GetAvailability, thay vì để AI tự đoán/bịa giờ trống.
        private async Task<string> BuildAvailableSlotsContextAsync()
        {
            const string location = "Nhà thuốc TMPMS";
            var now = DateTime.Now;
            var today = now.Date;
            var rangeStart = today;
            var rangeEnd = today.AddDays(3); // exclusive — hôm nay + 2 ngày tới

            var occupied = new HashSet<DateTime>(await _context.Appointments
                .Where(a => a.Location == location && a.AppointmentDate >= rangeStart && a.AppointmentDate < rangeEnd
                    && new[] { "PendingConfirmation", "Confirmed", "CheckedIn", "AlternativeProposed" }.Contains(a.Status))
                .Select(a => a.AppointmentDate).ToListAsync());
            var held = new HashSet<DateTime>(await _context.AppointmentSlotHolds
                .Where(h => h.Location == location && !h.IsConsumed && h.ExpiresAt > DateTime.UtcNow
                    && h.AppointmentDate >= rangeStart && h.AppointmentDate < rangeEnd)
                .Select(h => h.AppointmentDate).ToListAsync());

            var lines = new List<string>();
            for (var day = rangeStart; day < rangeEnd; day = day.AddDays(1))
            {
                var freeSlots = new List<string>();
                for (var time = day.AddHours(8); time <= day.AddHours(17.5); time = time.AddMinutes(30))
                {
                    if (time > now && !occupied.Contains(time) && !held.Contains(time))
                        freeSlots.Add(time.ToString("HH:mm"));
                }
                var dayLabel = day == today ? "Hôm nay" : day == today.AddDays(1) ? "Ngày mai" : day.ToString("dddd", new System.Globalization.CultureInfo("vi-VN"));
                lines.Add(freeSlots.Count > 0
                    ? $"- {dayLabel} ({day:dd/MM}): {string.Join(", ", freeSlots)}"
                    : $"- {dayLabel} ({day:dd/MM}): hết chỗ");
            }
            return string.Join("\n", lines);
        }

        // Giữ 1 khung giờ khám cho trợ lý AI — cố ý mirror logic của AppointmentController.HoldSlot
        // (khung giờ hợp lệ, giới hạn 3 lịch đang hoạt động, transaction Serializable chống double-booking)
        // thay vì gọi chéo sang controller khác, để không đụng vào luồng thanh toán/đặt lịch đã ổn định.
        // Việc giữ chỗ này KHÔNG tốn tiền và tự hết hạn sau 15 phút nếu khách không hoàn tất đặt cọc,
        // nên an toàn để AI thực hiện ngay mà không cần khách xác nhận thêm.
        private async Task<(bool Ok, string? Error, object? Hold)> TryHoldAppointmentSlotAsync(int userId, DateTime appointmentDate, string? symptomHint)
        {
            const string location = "Nhà thuốc TMPMS";

            if (appointmentDate <= DateTime.Now || appointmentDate > DateTime.Now.AddDays(14))
                return (false, "Thời điểm bạn nêu không hợp lệ (đã qua hoặc quá xa) — bạn có thể chọn giờ khác qua trang đặt lịch.", null);
            if (appointmentDate.Minute is not (0 or 30) || appointmentDate.Hour < 8 || appointmentDate.TimeOfDay > TimeSpan.FromHours(17.5))
                return (false, "Khung giờ đó nằm ngoài giờ làm việc (08:00–17:30, theo mốc 30 phút) — bạn có thể chọn giờ khác qua trang đặt lịch.", null);

            var activeCount = await _context.Appointments.CountAsync(a => a.UserId == userId
                && new[] { "PendingConfirmation", "Confirmed", "CheckedIn", "AlternativeProposed", "RescheduleRequested" }.Contains(a.Status));
            if (activeCount >= 3)
                return (false, "Bạn đã có đủ 3 lịch đang hoạt động. Vui lòng hoàn thành hoặc hủy một lịch trước khi đặt tiếp.", null);

            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await _context.AppointmentSlotHolds.Where(h => !h.IsConsumed && h.ExpiresAt <= DateTime.UtcNow)
                .ExecuteUpdateAsync(s => s.SetProperty(h => h.IsConsumed, true));
            var unavailable = await _context.Appointments.AnyAsync(a => a.Location == location && a.AppointmentDate == appointmentDate
                    && new[] { "PendingConfirmation", "Confirmed", "CheckedIn", "AlternativeProposed" }.Contains(a.Status))
                || await _context.AppointmentSlotHolds.AnyAsync(h => h.Location == location && h.AppointmentDate == appointmentDate && !h.IsConsumed && h.ExpiresAt > DateTime.UtcNow);
            if (unavailable)
                return (false, "Khung giờ đó vừa có người khác giữ chỗ. Bạn có thể chọn giờ khác qua trang đặt lịch.", null);

            var hold = new AppointmentSlotHold { UserId = userId, AppointmentDate = appointmentDate, Location = location, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(15) };
            _context.AppointmentSlotHolds.Add(hold);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, null, new
            {
                token = hold.Token,
                expiresAt = hold.ExpiresAt,
                appointmentDate = hold.AppointmentDate,
                location = hold.Location,
                depositAmount = DefaultAppointmentDeposit,
                symptomHint
            });
        }
    }
}
