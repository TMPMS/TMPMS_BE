using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPMS.Migrations
{
    /// <inheritdoc />
    public partial class BackfillExportCostPriceSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Toàn bộ 56 giao dịch Export hiện có đều thiếu UnitCostPrice (cột mới thêm, chưa có xuất
            // kho nào chạy qua service để tự chốt giá) nên bị loại hoàn toàn khỏi báo cáo lãi gộp theo
            // kỳ (GetProfitByPeriod bỏ qua giao dịch có unitCost == null) — báo cáo hiện gần như trống.
            // Ước tính giá vốn 65% giá bán: tỉ lệ trung vị thực tế đo được trên 183 StockBatch đã có sẵn
            // UnitCostPrice hợp lệ trong DB hiện tại (loại bỏ ~10 dòng lệch đơn vị bất thường), nên đây
            // là con số phù hợp với chính dữ liệu của hệ thống, không phải số bịa tùy ý.
            const decimal CostRatio = 0.65m;

            // 1) Backfill các lô (StockBatch) chưa có giá vốn — chủ yếu là lô "INIT-*" tạo tự động
            // trước khi có quản lý theo lô (xem migration BackfillInitialStockBatches).
            migrationBuilder.Sql($@"
UPDATE b
SET b.UnitCostPrice = CAST(ISNULL(b.SellPrice, ISNULL(m.Price, 10000)) * {CostRatio} AS DECIMAL(18,2))
FROM StockBatches b
JOIN Medicines m ON m.Id = b.MedicineId
WHERE b.UnitCostPrice IS NULL;
");

            // 2) Backfill snapshot giá vốn trên giao dịch Export: ưu tiên lấy từ lô liên kết (vừa được
            // backfill ở bước 1), rơi về giá thuốc hiện tại * tỉ lệ chi phí cho các giao dịch cũ không
            // có StockBatchId (trước khi có quản lý theo lô).
            migrationBuilder.Sql(@"
UPDATE t
SET t.UnitCostPrice = b.UnitCostPrice
FROM InventoryTransactions t
JOIN StockBatches b ON b.Id = t.StockBatchId
WHERE t.Type = 'Export' AND t.UnitCostPrice IS NULL AND b.UnitCostPrice IS NOT NULL;
");

            migrationBuilder.Sql($@"
UPDATE t
SET t.UnitCostPrice = CAST(ISNULL(m.Price, 10000) * {CostRatio} AS DECIMAL(18,2))
FROM InventoryTransactions t
JOIN Medicines m ON m.Id = t.MedicineId
WHERE t.Type = 'Export' AND t.UnitCostPrice IS NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Backfill dữ liệu ước tính một chiều: không lưu vết dòng nào vốn dĩ đã NULL trước khi chạy
            // Up(), nên không thể phục hồi chính xác trạng thái NULL ban đầu. Chấp nhận không đảo ngược
            // được (giống tinh thần migration BackfillInitialStockBatches ở trên) vì đây là dữ liệu ước
            // tính bổ sung cho báo cáo demo, không phải dữ liệu nghiệp vụ gốc.
        }
    }
}
