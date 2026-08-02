using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMPMS.Data;
using TMPMS.Models;
using System.Linq;
using System.Threading.Tasks;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoresController : ControllerBase
    {
        private readonly TMPMSDbContext _context;

        public StoresController(TMPMSDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetStores([FromQuery] string? provinceId, [FromQuery] string? districtId, [FromQuery] string? search)
        {
            var query = _context.Stores.Where(s => s.IsActive).AsQueryable();

            if (!string.IsNullOrWhiteSpace(provinceId))
            {
                query = query.Where(s => s.ProvinceId.ToLower() == provinceId.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(districtId) && districtId.ToLower() != "all")
            {
                query = query.Where(s => s.DistrictId.ToLower() == districtId.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower().Trim();
                query = query.Where(s => s.Name.ToLower().Contains(lowerSearch) || s.Address.ToLower().Contains(lowerSearch));
            }

            var stores = await query.ToListAsync();
            return Ok(stores);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStore(int id)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null || !store.IsActive) return NotFound();
            return Ok(store);
        }
    }
}
