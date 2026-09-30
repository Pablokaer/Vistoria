using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InspectFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "ai");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "companies");

            migrationBuilder.EnsureSchema(
                name: "inspections");

            migrationBuilder.EnsureSchema(
                name: "reports");

            migrationBuilder.EnsureSchema(
                name: "media");

            migrationBuilder.EnsureSchema(
                name: "notifications");

            migrationBuilder.EnsureSchema(
                name: "properties");

            migrationBuilder.EnsureSchema(
                name: "tenancies");

            migrationBuilder.EnsureSchema(
                name: "tenants");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "companies",
                schema: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    link = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "properties",
                schema: "properties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address_line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    postcode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    property_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_properties", x => x.id);
                    table.ForeignKey(
                        name: "fk_properties_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "companies",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_claims_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "identity",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_profiles",
                schema: "identity",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    service_area = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_profiles", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_agent_profiles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "company_members",
                schema: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_company_members_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "companies",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_company_members_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_profiles",
                schema: "tenants",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_profiles", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_tenant_profiles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_claims_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                schema: "identity",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_user_logins_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "identity",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "identity",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                schema: "identity",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_user_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "property_rooms",
                schema: "properties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_property_rooms", x => x.id);
                    table.ForeignKey(
                        name: "fk_property_rooms_properties_property_id",
                        column: x => x.property_id,
                        principalSchema: "properties",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenancies",
                schema: "tenancies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenancies", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenancies_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "companies",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenancies_properties_property_id",
                        column: x => x.property_id,
                        principalSchema: "properties",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inspections",
                schema: "inspections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenancy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inspection_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    visibility = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    comparison_inspection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    scheduled_date = table.Column<DateOnly>(type: "date", nullable: true),
                    accept_by = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    review_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    sent_to_tenant_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_responded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_responded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    expired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inspections", x => x.id);
                    table.ForeignKey(
                        name: "fk_inspections_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "companies",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inspections_inspections_comparison_inspection_id",
                        column: x => x.comparison_inspection_id,
                        principalSchema: "inspections",
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inspections_properties_property_id",
                        column: x => x.property_id,
                        principalSchema: "properties",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inspections_tenancies_tenancy_id",
                        column: x => x.tenancy_id,
                        principalSchema: "tenancies",
                        principalTable: "tenancies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inspections_users_agent_id",
                        column: x => x.agent_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenancy_members",
                schema: "tenancies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenancy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    invitation_token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    invitation_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    invited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenancy_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenancy_members_tenancies_tenancy_id",
                        column: x => x.tenancy_id,
                        principalSchema: "tenancies",
                        principalTable: "tenancies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tenancy_members_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inspection_invitations",
                schema: "inspections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    access_code_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    invited_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    last_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    used_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "inspection_reports",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    latest_version_number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inspection_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_inspection_reports_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalSchema: "inspections",
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inspection_rooms",
                schema: "inspections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_property_room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    comparison_inspection_room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ai_description = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    latest_ai_analysis_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ai_description_generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    final_description = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    final_description_source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    final_description_edited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    final_description_edited_by = table.Column<Guid>(type: "uuid", nullable: true),
                    defects_found = table.Column<bool>(type: "boolean", nullable: false),
                    agent_notes = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inspection_rooms", x => x.id);
                    table.ForeignKey(
                        name: "fk_inspection_rooms_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalSchema: "inspections",
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inspection_report_versions",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    snapshot_json = table.Column<string>(type: "text", nullable: false),
                    snapshot_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    pdf_storage_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    pdf_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    pdf_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    generated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inspection_report_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_inspection_report_versions_inspection_reports_report_id",
                        column: x => x.report_id,
                        principalSchema: "reports",
                        principalTable: "inspection_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "report_share_links",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_accessed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_share_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_share_links_inspection_reports_report_id",
                        column: x => x.report_id,
                        principalSchema: "reports",
                        principalTable: "inspection_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_analyses",
                schema: "ai",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    defect_id = table.Column<Guid>(type: "uuid", nullable: true),
                    comparison_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    prompt_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    media_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    result_json = table.Column<string>(type: "jsonb", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    confidence = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: true),
                    error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_analyses", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_analyses_inspection_rooms_inspection_room_id",
                        column: x => x.inspection_room_id,
                        principalSchema: "inspections",
                        principalTable: "inspection_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inspection_comparisons",
                schema: "inspections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_inspection_room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    basic_comparison = table.Column<string>(type: "text", nullable: true),
                    ai_analysis = table.Column<string>(type: "text", nullable: true),
                    latest_ai_analysis_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agent_decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    agent_notes = table.Column<string>(type: "text", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inspection_comparisons", x => x.id);
                    table.ForeignKey(
                        name: "fk_inspection_comparisons_inspection_rooms_inspection_room_id",
                        column: x => x.inspection_room_id,
                        principalSchema: "inspections",
                        principalTable: "inspection_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inspection_comparisons_inspection_rooms_source_inspection_r",
                        column: x => x.source_inspection_room_id,
                        principalSchema: "inspections",
                        principalTable: "inspection_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inspection_comparisons_inspections_source_inspection_id",
                        column: x => x.source_inspection_id,
                        principalSchema: "inspections",
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inspection_comparisons_inspections_target_inspection_id",
                        column: x => x.target_inspection_id,
                        principalSchema: "inspections",
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inspection_defects",
                schema: "inspections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    classification = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ai_description = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    ai_confidence = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: true),
                    latest_ai_analysis_id = table.Column<Guid>(type: "uuid", nullable: true),
                    final_description = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    agent_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inspection_defects", x => x.id);
                    table.ForeignKey(
                        name: "fk_inspection_defects_inspection_rooms_inspection_room_id",
                        column: x => x.inspection_room_id,
                        principalSchema: "inspections",
                        principalTable: "inspection_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_observations",
                schema: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_observations", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_observations_inspection_report_versions_report_versi",
                        column: x => x.report_version_id,
                        principalSchema: "reports",
                        principalTable: "inspection_report_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_observations_inspection_rooms_inspection_room_id",
                        column: x => x.inspection_room_id,
                        principalSchema: "inspections",
                        principalTable: "inspection_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_observations_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalSchema: "inspections",
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_observations_users_author_user_id",
                        column: x => x.author_user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_responses",
                schema: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_responses", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_responses_inspection_report_versions_report_version_",
                        column: x => x.report_version_id,
                        principalSchema: "reports",
                        principalTable: "inspection_report_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_responses_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalSchema: "inspections",
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_responses_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inspection_room_media",
                schema: "media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    defect_id = table.Column<Guid>(type: "uuid", nullable: true),
                    media_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    original_filename = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    caption = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inspection_room_media", x => x.id);
                    table.ForeignKey(
                        name: "fk_inspection_room_media_inspection_defects_defect_id",
                        column: x => x.defect_id,
                        principalSchema: "inspections",
                        principalTable: "inspection_defects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inspection_room_media_inspection_rooms_inspection_room_id",
                        column: x => x.inspection_room_id,
                        principalSchema: "inspections",
                        principalTable: "inspection_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inspection_room_media_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalSchema: "inspections",
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_analyses_inspection_room_id_kind_status",
                schema: "ai",
                table: "ai_analyses",
                columns: new[] { "inspection_room_id", "kind", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_analyses_status",
                schema: "ai",
                table: "ai_analyses",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_type_entity_id",
                schema: "audit",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_timestamp",
                schema: "audit",
                table: "audit_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                schema: "audit",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_company_members_company_id_user_id",
                schema: "companies",
                table: "company_members",
                columns: new[] { "company_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_company_members_user_id",
                schema: "companies",
                table: "company_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_comparisons_inspection_room_id",
                schema: "inspections",
                table: "inspection_comparisons",
                column: "inspection_room_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inspection_comparisons_source_inspection_id",
                schema: "inspections",
                table: "inspection_comparisons",
                column: "source_inspection_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_comparisons_source_inspection_room_id",
                schema: "inspections",
                table: "inspection_comparisons",
                column: "source_inspection_room_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_comparisons_target_inspection_id",
                schema: "inspections",
                table: "inspection_comparisons",
                column: "target_inspection_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_defects_inspection_room_id",
                schema: "inspections",
                table: "inspection_defects",
                column: "inspection_room_id");

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

            migrationBuilder.CreateIndex(
                name: "ix_inspection_report_versions_report_id_version_number",
                schema: "reports",
                table: "inspection_report_versions",
                columns: new[] { "report_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inspection_reports_inspection_id",
                schema: "reports",
                table: "inspection_reports",
                column: "inspection_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inspection_reports_report_number",
                schema: "reports",
                table: "inspection_reports",
                column: "report_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inspection_room_media_defect_id",
                schema: "media",
                table: "inspection_room_media",
                column: "defect_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_room_media_inspection_id",
                schema: "media",
                table: "inspection_room_media",
                column: "inspection_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_room_media_inspection_room_id",
                schema: "media",
                table: "inspection_room_media",
                column: "inspection_room_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_room_media_storage_key",
                schema: "media",
                table: "inspection_room_media",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inspection_rooms_inspection_id",
                schema: "inspections",
                table: "inspection_rooms",
                column: "inspection_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspections_agent_id_status",
                schema: "inspections",
                table: "inspections",
                columns: new[] { "agent_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_inspections_company_id_status",
                schema: "inspections",
                table: "inspections",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_inspections_comparison_inspection_id",
                schema: "inspections",
                table: "inspections",
                column: "comparison_inspection_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspections_property_id",
                schema: "inspections",
                table: "inspections",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspections_status_visibility",
                schema: "inspections",
                table: "inspections",
                columns: new[] { "status", "visibility" });

            migrationBuilder.CreateIndex(
                name: "ix_inspections_tenancy_id",
                schema: "inspections",
                table: "inspections",
                column: "tenancy_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_recipient_user_id_created_at",
                schema: "notifications",
                table: "notifications",
                columns: new[] { "recipient_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_properties_company_id",
                schema: "properties",
                table: "properties",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_property_rooms_property_id",
                schema: "properties",
                table: "property_rooms",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_family_id",
                schema: "identity",
                table: "refresh_tokens",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                schema: "identity",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                schema: "identity",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_share_links_report_id",
                schema: "reports",
                table: "report_share_links",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_share_links_token_hash",
                schema: "reports",
                table: "report_share_links",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_claims_role_id",
                schema: "identity",
                table: "role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "identity",
                table: "roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenancies_company_id",
                schema: "tenancies",
                table: "tenancies",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenancies_property_id",
                schema: "tenancies",
                table: "tenancies",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenancy_members_invitation_token_hash",
                schema: "tenancies",
                table: "tenancy_members",
                column: "invitation_token_hash",
                unique: true,
                filter: "invitation_token_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tenancy_members_tenancy_id_email",
                schema: "tenancies",
                table: "tenancy_members",
                columns: new[] { "tenancy_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenancy_members_user_id",
                schema: "tenancies",
                table: "tenancy_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_observations_author_user_id",
                schema: "tenants",
                table: "tenant_observations",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_observations_inspection_id",
                schema: "tenants",
                table: "tenant_observations",
                column: "inspection_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_observations_inspection_room_id",
                schema: "tenants",
                table: "tenant_observations",
                column: "inspection_room_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_observations_report_version_id",
                schema: "tenants",
                table: "tenant_observations",
                column: "report_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_responses_inspection_id",
                schema: "tenants",
                table: "tenant_responses",
                column: "inspection_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_responses_report_version_id",
                schema: "tenants",
                table: "tenant_responses",
                column: "report_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_responses_user_id",
                schema: "tenants",
                table: "tenant_responses",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_claims_user_id",
                schema: "identity",
                table: "user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_logins_user_id",
                schema: "identity",
                table: "user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                schema: "identity",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "identity",
                table: "users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "users",
                column: "normalized_user_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_profiles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "ai_analyses",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "company_members",
                schema: "companies");

            migrationBuilder.DropTable(
                name: "inspection_comparisons",
                schema: "inspections");

            migrationBuilder.DropTable(
                name: "inspection_invitations",
                schema: "inspections");

            migrationBuilder.DropTable(
                name: "inspection_room_media",
                schema: "media");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "property_rooms",
                schema: "properties");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "report_share_links",
                schema: "reports");

            migrationBuilder.DropTable(
                name: "role_claims",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "tenancy_members",
                schema: "tenancies");

            migrationBuilder.DropTable(
                name: "tenant_observations",
                schema: "tenants");

            migrationBuilder.DropTable(
                name: "tenant_profiles",
                schema: "tenants");

            migrationBuilder.DropTable(
                name: "tenant_responses",
                schema: "tenants");

            migrationBuilder.DropTable(
                name: "user_claims",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_logins",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "inspection_defects",
                schema: "inspections");

            migrationBuilder.DropTable(
                name: "inspection_report_versions",
                schema: "reports");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "inspection_rooms",
                schema: "inspections");

            migrationBuilder.DropTable(
                name: "inspection_reports",
                schema: "reports");

            migrationBuilder.DropTable(
                name: "inspections",
                schema: "inspections");

            migrationBuilder.DropTable(
                name: "tenancies",
                schema: "tenancies");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "properties",
                schema: "properties");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "companies");
        }
    }
}
