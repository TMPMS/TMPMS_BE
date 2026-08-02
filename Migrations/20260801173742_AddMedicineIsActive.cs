using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPMS.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicineIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Medicines",
                type: "bit",
                nullable: false,
                defaultValue: true);

            // Đặt tất cả sản phẩm hiện có là IsActive = 1
            migrationBuilder.Sql("UPDATE [Medicines] SET [IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Medicines");
        }
    }
}
