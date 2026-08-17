using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPMS.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionItemUnitPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "PrescriptionItems",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "PrescriptionItems");
        }
    }
}
