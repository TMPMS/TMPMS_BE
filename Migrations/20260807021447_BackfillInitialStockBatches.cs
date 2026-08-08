using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPMS.Migrations
{
    /// <inheritdoc />
    public partial class BackfillInitialStockBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tạo lô "khởi tạo" cho tồn kho hiện có (trước khi có quản lý theo lô) tại kho mặc định,
            // giữ nguyên NSX/HSD đang lưu trên Medicine để không mất dữ liệu tồn kho cũ.
            migrationBuilder.Sql(@"
INSERT INTO StockBatches (MedicineId, WarehouseId, SupplierId, BatchNumber, ManufactureDate, ExpiryDate, QuantityReceived, QuantityRemaining, UnitCostPrice, ReceivedAt, Status, Note)
SELECT m.Id, w.Id, NULL, CONCAT('INIT-', m.Id), m.ManufactureDate, m.ExpiryDate, m.StockQuantity, m.StockQuantity, NULL, GETDATE(), 'Active', N'Lo khoi tao tu dong tu ton kho truoc khi ap dung quan ly theo lo'
FROM Medicines m
CROSS JOIN (SELECT TOP 1 Id FROM Warehouses ORDER BY Id) w
WHERE m.StockQuantity > 0;
");

            // Đồng bộ cache InventoryStocks theo tổng số lượng còn hạn của các lô vừa tạo
            migrationBuilder.Sql(@"
MERGE InventoryStocks AS target
USING (
    SELECT b.MedicineId, b.WarehouseId, SUM(b.QuantityRemaining) AS Total
    FROM StockBatches b
    WHERE b.Status = 'Active' AND b.ExpiryDate >= GETDATE()
    GROUP BY b.MedicineId, b.WarehouseId
) AS source
ON target.MedicineId = source.MedicineId AND target.WarehouseId = source.WarehouseId
WHEN MATCHED THEN UPDATE SET target.Quantity = source.Total
WHEN NOT MATCHED THEN INSERT (MedicineId, WarehouseId, Quantity) VALUES (source.MedicineId, source.WarehouseId, source.Total);
");

            // Đồng bộ Medicine.StockQuantity = tổng số lượng còn hạn trên tất cả các lô (0 nếu không còn lô nào còn hạn)
            migrationBuilder.Sql(@"
UPDATE m
SET m.StockQuantity = ISNULL(agg.Total, 0)
FROM Medicines m
LEFT JOIN (
    SELECT MedicineId, SUM(QuantityRemaining) AS Total
    FROM StockBatches
    WHERE Status = 'Active' AND ExpiryDate >= GETDATE()
    GROUP BY MedicineId
) agg ON agg.MedicineId = m.Id;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM StockBatches WHERE BatchNumber LIKE 'INIT-%';");
        }
    }
}
