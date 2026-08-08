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

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> Create([FromBody] DiagnosisCreateDTO dto)
        {
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
            return Ok(result);
        }

        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult> GetByPatient(int patientId) => Ok(await _service.GetByPatient(patientId));

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
