using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InspectFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyReportLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "report_language",
                schema: "companies",
                table: "companies",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "en");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "report_language",
                schema: "companies",
                table: "companies");
        }
    }
}
