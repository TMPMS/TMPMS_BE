using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using TMPMS.DTOs;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Accountant")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _service;
        public ReportController(IReportService service) => _service = service;

        [HttpPost("revenue")]
        public async Task<ActionResult> GetRevenueReport([FromBody] RevenueReportRequestDTO dto)
            => Ok(await _service.GetRevenueReport(dto));

        [HttpGet("top-selling")]
        public async Task<ActionResult> GetTopSellingMedicines(
            [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int top = 10)
            => Ok(await _service.GetTopSellingMedicines(from, to, top));

        [HttpGet("order-status")]
        public async Task<ActionResult> GetOrderStatusStatistics()
            => Ok(await _service.GetOrderStatusStatistics());

        [HttpGet("category-revenue")]
        public async Task<ActionResult> GetCategoryRevenue([FromQuery] DateTime from, [FromQuery] DateTime to)
            => Ok(await _service.GetCategoryRevenue(from, to));

        [HttpGet("dashboard")]
        public async Task<ActionResult> GetDashboardSummary() => Ok(await _service.GetDashboardSummary());
    }
}
