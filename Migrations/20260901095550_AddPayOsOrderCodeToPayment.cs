using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPMS.Migrations
{
    /// <inheritdoc />
    public partial class AddPayOsOrderCodeToPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PayOsOrderCode",
                table: "Payments",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayOsOrderCode",
                table: "Payments");
        }
    }
}
