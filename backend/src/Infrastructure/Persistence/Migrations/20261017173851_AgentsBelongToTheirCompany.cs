using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InspectFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgentsBelongToTheirCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ADR 0022: Team and Private become Public (open to the company's agents) or Direct (one agent chosen).
            // An inspection that already has its agent keeps it; an open Private one waiting for an outside
            // inspector's code becomes visible to the company's own team instead.
            migrationBuilder.Sql("""
                UPDATE inspections.inspections SET visibility = 'Direct' WHERE visibility IN ('Team', 'Private') AND agent_id IS NOT NULL;
                UPDATE inspections.inspections SET visibility = 'Public' WHERE visibility IN ('Team', 'Private');
                """);

            migrationBuilder.DropTable(
                name: "inspection_invitations",
                schema: "inspections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inspection_invitations",
                schema: "inspections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_code_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    last_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    used_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inspection_invitations", x => x.id);
                    table.ForeignKey(
                        name: "fk_inspection_invitations_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalSchema: "inspections",
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inspection_invitations_inspection_id",
                schema: "inspections",
                table: "inspection_invitations",
                column: "inspection_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_invitations_token_hash",
                schema: "inspections",
                table: "inspection_invitations",
                column: "token_hash",
                unique: true);

            // Direct inspections go back to Team (the closest earlier meaning); invitations are not restored.
            migrationBuilder.Sql("UPDATE inspections.inspections SET visibility = 'Team' WHERE visibility = 'Direct';");
        }
    }
}
