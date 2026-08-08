using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPMS.Migrations
{
    /// <inheritdoc />
    public partial class AddHerbalInteractions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HerbalInteractions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HerbAId = table.Column<int>(type: "int", nullable: false),
                    HerbBId = table.Column<int>(type: "int", nullable: false),
                    InteractionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MechanismDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SuggestedReplacementForAId = table.Column<int>(type: "int", nullable: true),
                    SuggestedReplacementForBId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HerbalInteractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HerbalInteractions_Medicines_HerbAId",
                        column: x => x.HerbAId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HerbalInteractions_Medicines_HerbBId",
                        column: x => x.HerbBId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HerbalInteractions_Medicines_SuggestedReplacementForAId",
                        column: x => x.SuggestedReplacementForAId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HerbalInteractions_Medicines_SuggestedReplacementForBId",
                        column: x => x.SuggestedReplacementForBId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HerbalInteractions_HerbAId_HerbBId",
                table: "HerbalInteractions",
                columns: new[] { "HerbAId", "HerbBId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HerbalInteractions_HerbBId",
                table: "HerbalInteractions",
                column: "HerbBId");

            migrationBuilder.CreateIndex(
                name: "IX_HerbalInteractions_SuggestedReplacementForAId",
                table: "HerbalInteractions",
                column: "SuggestedReplacementForAId");

            migrationBuilder.CreateIndex(
                name: "IX_HerbalInteractions_SuggestedReplacementForBId",
                table: "HerbalInteractions",
                column: "SuggestedReplacementForBId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HerbalInteractions");
        }
    }
}
