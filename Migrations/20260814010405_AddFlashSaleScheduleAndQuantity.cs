using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPMS.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashSaleScheduleAndQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "FlashSales",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityLimit",
                table: "FlashSales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantitySold",
                table: "FlashSales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartTime",
                table: "FlashSales",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "FlashSales");

            migrationBuilder.DropColumn(
                name: "QuantityLimit",
                table: "FlashSales");

            migrationBuilder.DropColumn(
                name: "QuantitySold",
                table: "FlashSales");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "FlashSales");
        }
    }
}
