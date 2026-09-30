using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InspectFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.CreateTable(
                name: "billing_events",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider_event_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_billing_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider_customer_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    provider_subscription_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    current_period_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    current_period_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    past_due_since = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscriptions_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "checkout_sessions",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider_session_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    checkout_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checkout_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_checkout_sessions_subscriptions_subscription_id",
                        column: x => x.subscription_id,
                        principalSchema: "billing",
                        principalTable: "subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_billing_events_provider_provider_event_id",
                schema: "billing",
                table: "billing_events",
                columns: new[] { "provider", "provider_event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_checkout_sessions_provider_provider_session_id",
                schema: "billing",
                table: "checkout_sessions",
                columns: new[] { "provider", "provider_session_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_checkout_sessions_user_id",
                schema: "billing",
                table: "checkout_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_checkout_sessions_subscription_open",
                schema: "billing",
                table: "checkout_sessions",
                column: "subscription_id",
                unique: true,
                filter: "status = 'Open'");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_provider_provider_subscription_id",
                schema: "billing",
                table: "subscriptions",
                columns: new[] { "provider", "provider_subscription_id" });

            migrationBuilder.CreateIndex(
                name: "ux_subscriptions_user_open",
                schema: "billing",
                table: "subscriptions",
                column: "user_id",
                unique: true,
                filter: "status IN ('Pending', 'Active', 'PastDue')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "billing_events",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "checkout_sessions",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "subscriptions",
                schema: "billing");
        }
    }
}
