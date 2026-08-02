using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPMS.Migrations
{
    /// <inheritdoc />
    public partial class AddSymptomDiagnosisSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DoctorId",
                table: "Diagnoses",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "PrimarySyndromeId",
                table: "Diagnoses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScoreSnapshotJson",
                table: "Diagnoses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecondarySyndromeId",
                table: "Diagnoses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SymptomQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionOrder = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SymptomQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyndromeTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecommendationText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyndromeTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnswerOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    OptionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerOptions_SymptomQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "SymptomQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnswerScoreMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnswerOptionId = table.Column<int>(type: "int", nullable: false),
                    SyndromeTypeId = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerScoreMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerScoreMappings_AnswerOptions_AnswerOptionId",
                        column: x => x.AnswerOptionId,
                        principalTable: "AnswerOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnswerScoreMappings_SyndromeTypes_SyndromeTypeId",
                        column: x => x.SyndromeTypeId,
                        principalTable: "SyndromeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiagnosisAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiagnosisId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    AnswerOptionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosisAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiagnosisAnswers_AnswerOptions_AnswerOptionId",
                        column: x => x.AnswerOptionId,
                        principalTable: "AnswerOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiagnosisAnswers_Diagnoses_DiagnosisId",
                        column: x => x.DiagnosisId,
                        principalTable: "Diagnoses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiagnosisAnswers_SymptomQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "SymptomQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_PrimarySyndromeId",
                table: "Diagnoses",
                column: "PrimarySyndromeId");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_SecondarySyndromeId",
                table: "Diagnoses",
                column: "SecondarySyndromeId");

            migrationBuilder.CreateIndex(
                name: "IX_AnswerOptions_QuestionId",
                table: "AnswerOptions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AnswerScoreMappings_AnswerOptionId",
                table: "AnswerScoreMappings",
                column: "AnswerOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_AnswerScoreMappings_SyndromeTypeId",
                table: "AnswerScoreMappings",
                column: "SyndromeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisAnswers_AnswerOptionId",
                table: "DiagnosisAnswers",
                column: "AnswerOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisAnswers_DiagnosisId",
                table: "DiagnosisAnswers",
                column: "DiagnosisId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisAnswers_QuestionId",
                table: "DiagnosisAnswers",
                column: "QuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Diagnoses_SyndromeTypes_PrimarySyndromeId",
                table: "Diagnoses",
                column: "PrimarySyndromeId",
                principalTable: "SyndromeTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Diagnoses_SyndromeTypes_SecondarySyndromeId",
                table: "Diagnoses",
                column: "SecondarySyndromeId",
                principalTable: "SyndromeTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Diagnoses_SyndromeTypes_PrimarySyndromeId",
                table: "Diagnoses");

            migrationBuilder.DropForeignKey(
                name: "FK_Diagnoses_SyndromeTypes_SecondarySyndromeId",
                table: "Diagnoses");

            migrationBuilder.DropTable(
                name: "AnswerScoreMappings");

            migrationBuilder.DropTable(
                name: "DiagnosisAnswers");

            migrationBuilder.DropTable(
                name: "SyndromeTypes");

            migrationBuilder.DropTable(
                name: "AnswerOptions");

            migrationBuilder.DropTable(
                name: "SymptomQuestions");

            migrationBuilder.DropIndex(
                name: "IX_Diagnoses_PrimarySyndromeId",
                table: "Diagnoses");

            migrationBuilder.DropIndex(
                name: "IX_Diagnoses_SecondarySyndromeId",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "PrimarySyndromeId",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "ScoreSnapshotJson",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "SecondarySyndromeId",
                table: "Diagnoses");

            migrationBuilder.AlterColumn<int>(
                name: "DoctorId",
                table: "Diagnoses",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
