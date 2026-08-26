using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPMS.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMedicalHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MedicalHistory",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MedicalHistory",
                table: "AspNetUsers");
        }
    }
}
