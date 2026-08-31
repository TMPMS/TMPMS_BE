using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Services.Interfaces;
using TMPMS.DTOs;
using TMPMS.Utils;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [Route("[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Staff,Pharmacy,Accountant")]
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

        [HttpGet("staff-revenue")]
        public async Task<ActionResult> GetStaffRevenue([FromQuery] DateTime from, [FromQuery] DateTime to)
            => Ok(await _service.GetStaffRevenue(from, to));

        [HttpGet("appointment-status")]
        public async Task<ActionResult> GetAppointmentStatusStatistics()
            => Ok(await _service.GetAppointmentStatusStatistics());

        [HttpGet("prescription-status")]
        public async Task<ActionResult> GetPrescriptionStatusStatistics()
            => Ok(await _service.GetPrescriptionStatusStatistics());

        [HttpGet("user-growth")]
        public async Task<ActionResult> GetUserGrowth(
            [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string groupBy = "Day")
            => Ok(await _service.GetUserGrowth(from, to, groupBy));

        // Giá trị tồn kho hiện tại (giá vốn) theo từng kho — dùng cho biểu đồ chi tiết ở Dashboard.
        [HttpGet("inventory-value")]
        public async Task<ActionResult> GetInventoryValueByWarehouse()
            => Ok(await _service.GetInventoryValueByWarehouse());

        // GET /api/Report/revenue/export-excel?from=...&to=...&groupBy=Day
        // Xuất báo cáo doanh thu (theo khoảng ngày) ra file Excel gồm 4 sheet: Doanh thu, Bán chạy,
        // Theo danh mục, Theo nhân viên.
        [HttpGet("revenue/export-excel")]
        public async Task<IActionResult> ExportRevenueExcel(
            [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string groupBy = "Day")
        {
            var revenue = await _service.GetRevenueReport(new RevenueReportRequestDTO { FromDate = from, ToDate = to, GroupBy = groupBy });
            var topSelling = await _service.GetTopSellingMedicines(from, to, 20);
            var categoryRevenue = await _service.GetCategoryRevenue(from, to);
            var staffRevenue = await _service.GetStaffRevenue(from, to);

            var workbook = new XSSFWorkbook();
            var headerStyle = CreateStyle(workbook, bold: true, bgColor: NPOI.HSSF.Util.HSSFColor.LightGreen.Index);

            void WriteSheet(string name, string[] headers, IEnumerable<object[]> rows)
            {
                var sheet = workbook.CreateSheet(name);
                var headerRow = sheet.CreateRow(0);
                for (int c = 0; c < headers.Length; c++)
                {
                    var cell = headerRow.CreateCell(c);
                    cell.SetCellValue(headers[c]);
                    cell.CellStyle = headerStyle;
                }
                int r = 1;
                foreach (var rowValues in rows)
                {
                    var row = sheet.CreateRow(r++);
                    for (int c = 0; c < rowValues.Length; c++)
                    {
                        var cell = row.CreateCell(c);
                        switch (rowValues[c])
                        {
                            case decimal dv: cell.SetCellValue((double)dv); break;
                            case int iv: cell.SetCellValue(iv); break;
                            default: cell.SetCellValue(ExcelCellSanitizer.SafeText(rowValues[c]?.ToString())); break;
                        }
                    }
                }
                // Không gọi sheet.AutoSizeColumn() ở đây: NPOI đo font qua GDI/SixLabors và cần font
                // hệ thống — container Linux (mcr.microsoft.com/dotnet/aspnet:8.0) không cài font nào,
                // nên AutoSizeColumn ném exception và làm cả request 500 dù dữ liệu vẫn hợp lệ.
                for (int c = 0; c < headers.Length; c++) sheet.SetColumnWidth(c, 20 * 256);
            }

            WriteSheet("Doanh thu", new[] { "Kỳ", "Doanh thu bán hàng", "Doanh thu đặt cọc khám", "Tổng doanh thu", "Số đơn hàng" },
                revenue.Select(r => new object[] { r.Period, r.ProductRevenue, r.AppointmentDepositRevenue, r.Revenue, r.OrderCount }));

            WriteSheet("Bán chạy", new[] { "Tên thuốc", "Số lượng đã bán", "Doanh thu" },
                topSelling.Select(t => new object[] { t.MedicineName, t.TotalQuantitySold, t.TotalRevenue }));

            WriteSheet("Theo danh mục", new[] { "Danh mục", "Số lượng đã bán", "Doanh thu" },
                categoryRevenue.Select(c => new object[] { c.CategoryName, c.TotalQuantitySold, c.TotalRevenue }));

            WriteSheet("Theo nhân viên", new[] { "Nhân viên", "Doanh thu bán hàng", "Doanh thu đặt cọc khám", "Tổng doanh thu" },
                staffRevenue.Select(s => new object[] { s.StaffName, s.ProductRevenue, s.AppointmentDepositRevenue, s.TotalRevenue }));

            using var ms = new MemoryStream();
            workbook.Write(ms, false);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"bao_cao_doanh_thu_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx");
        }

        private static ICellStyle CreateStyle(IWorkbook wb, bool bold = false, short bgColor = 0)
        {
            var style = wb.CreateCellStyle();
            var font = wb.CreateFont();
            font.IsBold = bold;
            style.SetFont(font);
            if (bgColor != 0)
            {
                style.FillForegroundColor = bgColor;
                style.FillPattern = FillPattern.SolidForeground;
            }
            return style;
        }
    }
}
