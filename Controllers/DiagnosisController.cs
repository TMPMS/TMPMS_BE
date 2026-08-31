using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TMPMS.DTOs;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class DiagnosisController : ControllerBase
    {
        private readonly IDiagnosisService _service;
        public DiagnosisController(IDiagnosisService service) => _service = service;

        [HttpGet("questions")]
        [AllowAnonymous]
        public async Task<ActionResult> GetQuestions()
        {
            var questions = await _service.GetQuestionsAsync();
            return Ok(questions);
        }

        // Câu hỏi tự chẩn đoán thích ứng: trả về câu hỏi tiếp theo có khả năng phân biệt tốt nhất
        // giữa các thể bệnh đang dẫn đầu dựa trên câu trả lời đã có, hoặc Done=true nếu đã đủ rõ ràng
        // để dừng sớm/hết câu hỏi.
        [HttpPost("next-question")]
        [AllowAnonymous]
        public async Task<ActionResult> NextQuestion([FromBody] NextQuestionRequestDTO dto)
        {
            var result = await _service.GetNextQuestionAsync(dto.Answers);
            return Ok(result);
        }

        [HttpPost("classify")]
        [AllowAnonymous]
        public async Task<ActionResult> Classify([FromBody] DiagnosisClassifyRequestDTO dto)
        {
            try
            {
                int? userId = null;
                var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("id")?.Value;
                if (int.TryParse(claim, out int parsedId) && parsedId > 0)
                {
                    userId = parsedId;
                }

                var result = await _service.ClassifyAsync(dto, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Nếu role là "User" (bệnh nhân tự đăng nhập), chỉ cho phép thao tác trên hồ sơ chẩn đoán của
        // CHÍNH MÌNH — cùng quy ước đã dùng ở PatientController (GetDiagnosisHistory/GetPrescriptionHistory).
        // Trước đây Create/GetById/GetByPatient không kiểm tra gì, cho phép đọc hồ sơ y tế của người khác
        // hoặc tạo chẩn đoán khống gán cho bệnh nhân bất kỳ (IDOR).
        private bool IsForbiddenForOtherPatient(int patientId)
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleClaim != "User") return false;
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !int.TryParse(idClaim, out var loggedInUserId) || loggedInUserId != patientId;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> Create([FromBody] DiagnosisCreateDTO dto)
        {
            if (IsForbiddenForOtherPatient(dto.PatientId)) return Forbid();
            try
            {
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
            if (IsForbiddenForOtherPatient(result.PatientId)) return Forbid();
            return Ok(result);
        }

        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult> GetByPatient(int patientId)
        {
            if (IsForbiddenForOtherPatient(patientId)) return Forbid();
            return Ok(await _service.GetByPatient(patientId));
        }

        [HttpGet("doctor/{doctorId}")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<ActionResult> GetByDoctor(int doctorId) => Ok(await _service.GetByDoctor(doctorId));

        [HttpGet]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<ActionResult> GetAll() => Ok(await _service.GetAll());

        [HttpPut("{id}")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<ActionResult> Update(int id, [FromBody] DiagnosisUpdateDTO dto)
        {
            var result = await _service.Update(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            var ok = await _service.Delete(id);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
