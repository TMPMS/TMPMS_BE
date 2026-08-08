using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPMS.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderProcessedByStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // LƯU Ý: migration này chỉ thêm Orders.ProcessedByStaffId. Các câu lệnh AddColumn/CreateTable
            // cho Appointments/AppointmentPayments/... mà `dotnet ef migrations add` sinh thêm đã bị loại bỏ
            // thủ công — chúng thuộc migration 20260807050000_AddAdvancedAppointmentBooking (đã áp dụng vào
            // DB thật), chỉ xuất hiện lại ở đây do ModelSnapshot.cs bị thiếu cập nhật ở commit đó.
            migrationBuilder.AddColumn<int>(
                name: "ProcessedByStaffId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ProcessedByStaffId",
                table: "Orders",
                column: "ProcessedByStaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_ProcessedByStaffId",
                table: "Orders",
                column: "ProcessedByStaffId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_ProcessedByStaffId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ProcessedByStaffId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProcessedByStaffId",
                table: "Orders");
        }
    }
}
