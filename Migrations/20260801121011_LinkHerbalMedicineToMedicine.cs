using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPMS.Migrations
{
    /// <inheritdoc />
    public partial class LinkHerbalMedicineToMedicine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HerbalMedicineInfos_Medicines_MedicineId",
                table: "HerbalMedicineInfos");

            migrationBuilder.DropIndex(
                name: "IX_HerbalMedicineInfos_MedicineId",
                table: "HerbalMedicineInfos");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Medicines",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "MedicineId",
                table: "HerbalMedicineInfos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_HerbalMedicineInfos_MedicineId",
                table: "HerbalMedicineInfos",
                column: "MedicineId",
                unique: true,
                filter: "[MedicineId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_HerbalMedicineInfos_Medicines_MedicineId",
                table: "HerbalMedicineInfos",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HerbalMedicineInfos_Medicines_MedicineId",
                table: "HerbalMedicineInfos");

            migrationBuilder.DropIndex(
                name: "IX_HerbalMedicineInfos_MedicineId",
                table: "HerbalMedicineInfos");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Medicines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MedicineId",
                table: "HerbalMedicineInfos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HerbalMedicineInfos_MedicineId",
                table: "HerbalMedicineInfos",
                column: "MedicineId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_HerbalMedicineInfos_Medicines_MedicineId",
                table: "HerbalMedicineInfos",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
