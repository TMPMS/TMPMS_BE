using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("[controller]")]
    public class HealthQuizController : ControllerBase
    {
        private readonly IHealthQuizService _service;

        public HealthQuizController(IHealthQuizService service)
        {
            _service = service;
        }

        [HttpGet("list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveQuizzes()
        {
            var result = await _service.GetActiveQuizzesAsync();
            return Ok(result);
        }

        [HttpGet("{code}/questions")]
        [AllowAnonymous]
        public async Task<IActionResult> GetQuizQuestions(string code)
        {
            var quiz = await _service.GetQuizByCodeAsync(code);
            if (quiz == null)
            {
                return NotFound(new { message = $"Không tìm thấy bài test với mã '{code}'." });
            }
            return Ok(quiz);
        }

        [HttpPost("{code}/submit")]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitQuiz(string code, [FromBody] QuizSubmitRequestDTO dto)
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

                var result = await _service.SubmitQuizAsync(code, dto, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
