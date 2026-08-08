using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("[controller]")]
    public class MeridianAnalysisController : ControllerBase
    {
        private readonly IMeridianAnalysisService _service;
        public MeridianAnalysisController(IMeridianAnalysisService service) => _service = service;

        [HttpGet("{medicineId}")]
        public async Task<ActionResult> Get(int medicineId)
        {
            var result = await _service.GetAsync(medicineId);
            return Ok(result);
        }
    }
}
