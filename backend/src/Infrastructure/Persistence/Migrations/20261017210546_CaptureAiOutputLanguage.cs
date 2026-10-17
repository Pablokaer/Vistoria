using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InspectFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CaptureAiOutputLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "output_language",
                schema: "ai",
                table: "ai_analyses",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "en");

            // Earlier analyses were written in the company report language (ADR 0013); keep that on record.
            migrationBuilder.Sql("""
                UPDATE ai.ai_analyses a SET output_language = c.report_language
                FROM inspections.inspections i JOIN companies.companies c ON c.id = i.company_id
                WHERE i.id = a.inspection_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "output_language",
                schema: "ai",
                table: "ai_analyses");
        }
    }
}
