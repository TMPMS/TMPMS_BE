using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using TMPMS.DTOs;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [Route("[controller]")]
    [ApiController]
    [Authorize(Roles = "Pharmacy,Pharmacist,Admin,Doctor")]
    public class HerbalInteractionController : ControllerBase
    {
        private readonly IHerbalInteractionService _service;
        public HerbalInteractionController(IHerbalInteractionService service) => _service = service;

        [HttpPost("check-safety")]
        public async Task<ActionResult> CheckSafety([FromBody] SafetyCheckRequestDTO dto)
        {
            var result = await _service.CheckSafety(dto);
            return Ok(result);
        }
    }
}
