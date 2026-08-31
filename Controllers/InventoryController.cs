using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;
using TMPMS.Utils;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [Route("[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Warehouse,Pharmacy")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _service;
        private readonly IAuditLogService _auditLogService;
        public InventoryController(IInventoryService service, IAuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : (int?)null;
        }


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

        // ==== Quản lý theo lô (batch/lot) ====

        [HttpPost("batches")]
        public async Task<ActionResult> CreateBatch([FromBody] StockBatchCreateDTO dto)
        {
            try
            {
                var result = await _service.CreateBatch(dto);
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpGet("batches/medicine/{medicineId}")]
        public async Task<ActionResult> GetBatchesByMedicine(int medicineId, [FromQuery] int? warehouseId)
            => Ok(await _service.GetBatchesByMedicine(medicineId, warehouseId));

        [HttpGet("batches/warehouse/{warehouseId}")]
        public async Task<ActionResult> GetBatchesByWarehouse(int warehouseId)
            => Ok(await _service.GetBatchesByWarehouse(warehouseId));

        [HttpPost("batches/{id}/dispose")]
        public async Task<ActionResult> DisposeBatch(int id, [FromBody] BatchDisposeDTO dto)
        {
            try
            {
                var result = await _service.DisposeBatch(id, dto);
                await this.LogAuditAsync(_auditLogService, "StockBatch", "Dispose", id.ToString(), $"Huỷ lô hàng #{id} — lý do: {dto.Reason}");
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPatch("batches/{id}/adjust")]
        public async Task<ActionResult> AdjustBatch(int id, [FromBody] BatchAdjustDTO dto)
        {
            try
            {
                var result = await _service.AdjustBatch(id, dto);
                await this.LogAuditAsync(_auditLogService, "StockBatch", "Adjust", id.ToString(), $"Điều chỉnh lô hàng #{id} — số lượng còn lại mới: {dto.QuantityRemaining} — lý do: {dto.Reason}");
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        // ==== Flash Sale cho hàng gần hết hạn ====

        [HttpGet("flash-sale/candidates")]
        [AllowAnonymous]
        public async Task<ActionResult> GetFlashSaleCandidates([FromQuery] int daysThreshold = 30)
            => Ok(await _service.GetFlashSaleCandidates(daysThreshold));

        [HttpPost("flash-sale/{medicineId}/apply")]
        public async Task<ActionResult> ApplyFlashSale(int medicineId, [FromBody] ApplyFlashSaleDTO dto)
        {
            try
            {
                var result = await _service.ApplyFlashSale(medicineId, dto, GetUserId());
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("flash-sale/{medicineId}/remove")]
        public async Task<ActionResult> RemoveFlashSale(int medicineId)
        {
            try
            {
                await _service.RemoveFlashSale(medicineId, GetUserId());
                return Ok(new { message = "Đã gỡ Flash Sale." });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        // Bảng quản lý Flash Sale cho Admin — lịch sử áp dụng (mặc định chỉ các bản ghi đang bật).
        [HttpGet("flash-sale/list")]
        public async Task<ActionResult> GetFlashSaleList([FromQuery] bool activeOnly = true)
            => Ok(await _service.GetFlashSales(activeOnly));

        // Danh sách Flash Sale công khai (đang chạy + sắp diễn ra) cho trang khách hàng — không giới
        // hạn theo hàng sắp hết hạn như /candidates, gồm mọi sản phẩm Admin đã đưa vào Flash Sale.
        [HttpGet("flash-sale/active")]
        [AllowAnonymous]
        public async Task<ActionResult> GetActiveFlashSalesForCustomer()
            => Ok(await _service.GetActiveFlashSalesForCustomer());

        // ==== Báo cáo lãi gộp ước tính theo lô ====

        [HttpGet("reports/profit")]
        public async Task<ActionResult> GetBatchProfitReport([FromQuery] int? warehouseId, [FromQuery] int? medicineId)
            => Ok(await _service.GetBatchProfitReport(warehouseId, medicineId));

        // Lãi gộp tổng hợp theo kỳ, gộp mọi sản phẩm — bổ sung cho /reports/profit (vốn chỉ xem được
        // từng sản phẩm một), để có 1 màn hình xem lãi gộp toàn cửa hàng theo ngày/tháng/năm.
        [HttpGet("reports/profit-summary")]
        public async Task<ActionResult> GetProfitSummary([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string groupBy = "Day")
            => Ok(await _service.GetProfitByPeriod(from, to, groupBy));

        // Gợi ý nhập hàng dựa trên tốc độ bán gần đây — xem giải thích ở InventoryService.GetReorderSuggestions.
        [HttpGet("reports/reorder-suggestions")]
        public async Task<ActionResult> GetReorderSuggestions([FromQuery] int lookbackDays = 30, [FromQuery] int leadTimeDays = 30)
            => Ok(await _service.GetReorderSuggestions(lookbackDays, leadTimeDays));
    }
}
