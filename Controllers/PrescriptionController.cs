using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;
using TMPMS.DTOs;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class PrescriptionController : ControllerBase
    {
        private readonly IPrescriptionService _service;
        private readonly IWebHostEnvironment _environment;
        public PrescriptionController(IPrescriptionService service, IWebHostEnvironment environment)
        {
            _service = service;
            _environment = environment;
        }

        // Khách hàng tải ảnh toa thuốc thật lên (dùng cho tính năng "Gửi Toa Thuốc") — miễn phí,
        // không cần đặt lịch khám. Ảnh sẽ được Dược sĩ xem & AI hỗ trợ đọc trong màn hình duyệt đơn.
        [HttpPost("upload-image")]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest(new { message = "Vui lòng chọn ảnh." });
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) return BadRequest(new { message = "Chỉ hỗ trợ JPG, PNG hoặc WEBP." });
            var root = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder = Path.Combine(root, "uploads", "prescriptions");
            Directory.CreateDirectory(folder);
            var name = $"{Guid.NewGuid():N}{ext}";
            await using var stream = System.IO.File.Create(Path.Combine(folder, name));
            await file.CopyToAsync(stream);
            return Ok(new { url = $"/uploads/prescriptions/{name}" });
        }

        // Đọc ảnh toa thuốc bằng AI và gợi ý khớp với danh mục dược phẩm — chỉ Dược sĩ/Admin xem để duyệt đơn.
        [HttpPost("{id}/scan-image")]
        [Authorize(Roles = "Pharmacy,Admin")]
        public async Task<ActionResult> ScanImage(int id)
        {
            try { return Ok(await _service.ScanImage(id)); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        // Dược sĩ hoàn thiện (kê đơn) một đơn thuốc "Pending" gửi kèm ảnh: gắn thuốc + trừ kho + duyệt.
        [HttpPut("{id}/finalize")]
        [Authorize(Roles = "Pharmacy,Admin")]
        public async Task<ActionResult> Finalize(int id, [FromBody] PrescriptionFinalizeDTO dto)
        {
            try { return Ok(await _service.Finalize(id, dto)); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] PrescriptionCreateDTO dto)
        {
            try
            {
                // Bệnh nhân/khách hàng tự gửi toa: bắt buộc gắn vào chính tài khoản đang đăng nhập,
                // không tin dữ liệu UserId/PatientId client gửi lên. Dược sĩ sẽ đối chiếu & xác nhận
                // đúng bệnh nhân (PatientId) khi hoàn thiện đơn (Finalize) nếu gửi hộ người khác.
                if (!CanProxy())
                {
                    dto.UserId = GetCurrentUserId();
                    dto.PatientId = null;
                }
                var result = await _service.Create(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var result = await _service.GetById(id);
            if (result == null) return NotFound();
            if (!CanProxy() && result.UserId != GetCurrentUserId()) return Forbid();
            return Ok(result);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult> GetByUser(int userId)
        {
            if (!CanProxy() && userId != GetCurrentUserId()) return Forbid();
            return Ok(await _service.GetByUser(userId));
        }

        [HttpGet("status/{status}")]
        [Authorize(Roles = "Pharmacy,Admin")]
        public async Task<ActionResult> GetByStatus(string status) => Ok(await _service.GetByStatus(status));

        [HttpGet]
        [Authorize(Roles = "Pharmacy,Admin")]
        public async Task<ActionResult> GetAll() => Ok(await _service.GetAll());

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Pharmacy,Admin")]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] PrescriptionStatusUpdateDTO dto)
        {
            try
            {
                var result = await _service.UpdateStatus(id, dto);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            var ok = await _service.Delete(id);
            if (!ok) return NotFound();
            return NoContent();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(claim, out int userId);
            return userId;
        }

        private bool CanProxy()
        {
            return User.IsInRole("Admin") || User.IsInRole("Staff") || User.IsInRole("Pharmacy");
        }
    }
}
