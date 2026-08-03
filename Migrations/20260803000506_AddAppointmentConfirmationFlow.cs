using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPMS.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentConfirmationFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmationDeadline",
                table: "Appointments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedByStaffId",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ConfirmedByStaffId",
                table: "Appointments",
                column: "ConfirmedByStaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AspNetUsers_ConfirmedByStaffId",
                table: "Appointments",
                column: "ConfirmedByStaffId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AspNetUsers_ConfirmedByStaffId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ConfirmedByStaffId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ConfirmationDeadline",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ConfirmedByStaffId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Appointments");
        }
    }
}
