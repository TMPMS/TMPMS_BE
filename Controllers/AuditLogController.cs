using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;

namespace TMPMS.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _service;
        public AuditLogController(IAuditLogService service) => _service = service;

        [HttpGet("~/audit-logs")]
        [HttpGet("~/api/audit-logs")]
        [HttpGet("api/[controller]")]
        public async Task<IActionResult> Query([FromQuery] AuditLogQueryDto query)
            => Ok(await _service.QueryAsync(query));
    }
}
