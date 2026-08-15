using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("[controller]")]
    public class TongueAnalysisController : ControllerBase
    {
        private readonly ITongueAnalysisService _service;
        public TongueAnalysisController(ITongueAnalysisService service) => _service = service;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly (string ext, string mime)[] MimeMap =
        {
            (".jpg", "image/jpeg"), (".jpeg", "image/jpeg"), (".png", "image/png"), (".webp", "image/webp")
        };

        // Phân tích ảnh lưỡi (Thiệt chẩn) — công khai giống Diagnosis/classify, bổ trợ cho tự chẩn đoán
        // theo bảng câu hỏi, không yêu cầu đăng nhập.
        [HttpPost("analyze")]
        [AllowAnonymous]
        [RequestSizeLimit(5_000_000)]
        public async Task<ActionResult> Analyze(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Vui lòng chọn ảnh lưỡi." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return BadRequest(new { message = "Chỉ hỗ trợ ảnh JPG, PNG hoặc WEBP." });

            var mimeType = MimeMap.First(m => m.ext == ext).mime;

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            try
            {
                var result = await _service.AnalyzeAsync(ms.ToArray(), mimeType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
