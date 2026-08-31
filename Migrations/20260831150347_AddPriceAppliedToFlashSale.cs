using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPMS.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceAppliedToFlashSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue: true — backfill các Flash Sale đang có sẵn (đã áp giá từ trước) thành "đã áp
            // dụng", để SweepFlashSales không ép giá lại 1 lần ngay sau khi migrate (xem comment ở
            // FlashSale.PriceApplied / InventoryService.SweepFlashSales). Bản ghi mới tạo sau này luôn được
            // code gán giá trị đúng tường minh (ApplyFlashSale), không phụ thuộc default này.
            migrationBuilder.AddColumn<bool>(
                name: "PriceApplied",
                table: "FlashSales",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceApplied",
                table: "FlashSales");
        }
    }
}
