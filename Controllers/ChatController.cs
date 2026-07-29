using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using TMPMS.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly TMPMSDbContext _context;

        public ChatController(TMPMSDbContext context)
        {
            _context = context;
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

            object? recommendedProduct = null;
            if (!string.IsNullOrEmpty(queryTerm))
            {
                var medicine = await _context.Medicines
                    .FirstOrDefaultAsync(m => m.Name.Contains(queryTerm) || m.Description.Contains(queryTerm));

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
                text = replyText,
                product = recommendedProduct
            });
        }
    }
}
