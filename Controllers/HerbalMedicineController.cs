using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using TMPMS.DTOs;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [Route("[controller]")]
    [ApiController]
    public class HerbalMedicineController : ControllerBase
    {
        private readonly IHerbalMedicineService _service;
        public HerbalMedicineController(IHerbalMedicineService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult> GetAll() => Ok(await _service.GetAll());

        [HttpGet("{medicineId}")]
        public async Task<ActionResult> GetByMedicineId(int medicineId)
        {
            var result = await _service.GetByMedicineId(medicineId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create([FromBody] HerbalMedicineCreateDTO dto)
        {
            try
            {
                var result = await _service.Create(dto);
                return CreatedAtAction(nameof(GetByMedicineId), new { medicineId = result.MedicineId }, result);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPut("{medicineId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Update(int medicineId, [FromBody] HerbalMedicineUpdateDTO dto)
        {
            var result = await _service.Update(medicineId, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{medicineId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int medicineId)
        {
            var ok = await _service.Delete(medicineId);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
