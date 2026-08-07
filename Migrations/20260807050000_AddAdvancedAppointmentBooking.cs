using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TMPMS.Data;

#nullable disable

namespace TMPMS.Migrations
{
    [DbContext(typeof(TMPMSDbContext))]
    [Migration("20260807050000_AddAdvancedAppointmentBooking")]
    public partial class AddAdvancedAppointmentBooking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(name: "CancelledAt", table: "Appointments", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "CheckedInAt", table: "Appointments", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "CompletedAt", table: "Appointments", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "DepositAmount", table: "Appointments", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<string>(name: "Location", table: "Appointments", type: "nvarchar(max)", nullable: false, defaultValue: "Nhà thuốc TMPMS");
            migrationBuilder.AddColumn<string>(name: "PaymentMethod", table: "Appointments", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "PaymentStatus", table: "Appointments", type: "nvarchar(max)", nullable: false, defaultValue: "Unpaid");
            migrationBuilder.AddColumn<DateTime>(name: "PolicyAcceptedAt", table: "Appointments", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "PrescriptionImageUrl", table: "Appointments", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ProposedAppointmentDateNote", table: "Appointments", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "RefundAmount", table: "Appointments", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<string>(name: "SymptomDescription", table: "Appointments", type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AppointmentSlotHolds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false), StaffId = table.Column<int>(type: "int", nullable: true),
                    AppointmentDate = table.Column<DateTime>(type: "datetime2", nullable: false), Location = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(450)", nullable: false), ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false), IsConsumed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_AppointmentSlotHolds", x => x.Id); table.ForeignKey("FK_AppointmentSlotHolds_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade); });

            migrationBuilder.CreateTable(
                name: "AppointmentPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"), AppointmentId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false), Method = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false), TransactionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false), PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false), RefundStatus = table.Column<string>(type: "nvarchar(max)", nullable: true)
                }, constraints: table => { table.PrimaryKey("PK_AppointmentPayments", x => x.Id); table.ForeignKey("FK_AppointmentPayments_Appointments_AppointmentId", x => x.AppointmentId, "Appointments", "Id", onDelete: ReferentialAction.Cascade); });

            migrationBuilder.CreateTable(
                name: "AppointmentPaymentIntents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"), UserId = table.Column<int>(type: "int", nullable: false),
                    SlotHoldId = table.Column<int>(type: "int", nullable: false), OrderCode = table.Column<long>(type: "bigint", nullable: false),
                    SymptomDescription = table.Column<string>(type: "nvarchar(max)", nullable: false), PrescriptionImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true), Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false), PaymentLinkId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false), ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                }, constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentPaymentIntents", x => x.Id);
                    table.ForeignKey("FK_AppointmentPaymentIntents_AppointmentSlotHolds_SlotHoldId", x => x.SlotHoldId, "AppointmentSlotHolds", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_AppointmentPaymentIntents_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentRescheduleRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"), AppointmentId = table.Column<int>(type: "int", nullable: false),
                    OldAppointmentDate = table.Column<DateTime>(type: "datetime2", nullable: false), RequestedAppointmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false), Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false), ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true), ResolvedByStaffId = table.Column<int>(type: "int", nullable: true)
                }, constraints: table => { table.PrimaryKey("PK_AppointmentRescheduleRequests", x => x.Id); table.ForeignKey("FK_AppointmentRescheduleRequests_Appointments_AppointmentId", x => x.AppointmentId, "Appointments", "Id", onDelete: ReferentialAction.Cascade); });

            migrationBuilder.CreateIndex("IX_AppointmentSlotHolds_Token", "AppointmentSlotHolds", "Token", unique: true);
            migrationBuilder.CreateIndex("IX_AppointmentSlotHolds_UserId", "AppointmentSlotHolds", "UserId");
            migrationBuilder.CreateIndex("IX_AppointmentSlotHolds_AppointmentDate_Location", "AppointmentSlotHolds", new[] { "AppointmentDate", "Location" });
            migrationBuilder.CreateIndex("IX_AppointmentPayments_AppointmentId", "AppointmentPayments", "AppointmentId");
            migrationBuilder.CreateIndex("IX_AppointmentPaymentIntents_OrderCode", "AppointmentPaymentIntents", "OrderCode", unique: true);
            migrationBuilder.CreateIndex("IX_AppointmentPaymentIntents_SlotHoldId", "AppointmentPaymentIntents", "SlotHoldId");
            migrationBuilder.CreateIndex("IX_AppointmentPaymentIntents_UserId", "AppointmentPaymentIntents", "UserId");
            migrationBuilder.CreateIndex("IX_AppointmentRescheduleRequests_AppointmentId", "AppointmentRescheduleRequests", "AppointmentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("AppointmentPaymentIntents"); migrationBuilder.DropTable("AppointmentPayments"); migrationBuilder.DropTable("AppointmentRescheduleRequests"); migrationBuilder.DropTable("AppointmentSlotHolds");
            foreach (var column in new[] { "CancelledAt", "CheckedInAt", "CompletedAt", "DepositAmount", "Location", "PaymentMethod", "PaymentStatus", "PolicyAcceptedAt", "PrescriptionImageUrl", "ProposedAppointmentDateNote", "RefundAmount", "SymptomDescription" })
                migrationBuilder.DropColumn(column, "Appointments");
        }
    }
}
