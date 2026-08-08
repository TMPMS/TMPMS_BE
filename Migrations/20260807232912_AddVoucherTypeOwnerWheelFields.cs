using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPMS.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucherTypeOwnerWheelFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Vouchers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsWheelPrize",
                table: "Vouchers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Vouchers",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Vouchers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Weight",
                table: "Vouchers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductVoucherId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShippingVoucherId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WheelSpins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SpinDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VoucherId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WheelSpins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WheelSpins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WheelSpins_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_Code",
                table: "Vouchers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ProductVoucherId",
                table: "Orders",
                column: "ProductVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShippingVoucherId",
                table: "Orders",
                column: "ShippingVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_WheelSpins_UserId_SpinDate",
                table: "WheelSpins",
                columns: new[] { "UserId", "SpinDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WheelSpins_VoucherId",
                table: "WheelSpins",
                column: "VoucherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Vouchers_ProductVoucherId",
                table: "Orders",
                column: "ProductVoucherId",
                principalTable: "Vouchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Vouchers_ShippingVoucherId",
                table: "Orders",
                column: "ShippingVoucherId",
                principalTable: "Vouchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Vouchers_ProductVoucherId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Vouchers_ShippingVoucherId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "WheelSpins");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_Code",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ProductVoucherId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ShippingVoucherId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsWheelPrize",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ProductVoucherId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingVoucherId",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Vouchers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
