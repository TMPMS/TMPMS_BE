using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using TMPMS.DTOs;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Warehouse")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _service;
        public InventoryController(IInventoryService service) => _service = service;

        [HttpPost("transactions")]
        public async Task<ActionResult> CreateTransaction([FromBody] StockTransactionCreateDTO dto)
        {
            try
            {
                var result = await _service.CreateTransaction(dto);
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("transactions")]
        public async Task<ActionResult> GetTransactions([FromQuery] int? medicineId, [FromQuery] int? warehouseId)
            => Ok(await _service.GetTransactions(medicineId, warehouseId));

        [HttpGet("stock")]
        public async Task<ActionResult> GetAllStock() => Ok(await _service.GetAllStock());

        [HttpGet("stock/warehouse/{warehouseId}")]
        public async Task<ActionResult> GetStockByWarehouse(int warehouseId)
            => Ok(await _service.GetStockByWarehouse(warehouseId));

        [HttpGet("stock/medicine/{medicineId}")]
        public async Task<ActionResult> GetStockByMedicine(int medicineId)
            => Ok(await _service.GetStockByMedicine(medicineId));

        [HttpGet("alerts/low-stock")]
        public async Task<ActionResult> GetLowStockAlerts([FromQuery] int threshold = 20)
            => Ok(await _service.GetLowStockAlerts(threshold));

        [HttpGet("alerts/expiry")]
        public async Task<ActionResult> GetExpiryAlerts([FromQuery] int daysAhead = 30)
            => Ok(await _service.GetExpiryAlerts(daysAhead));
    }
}
