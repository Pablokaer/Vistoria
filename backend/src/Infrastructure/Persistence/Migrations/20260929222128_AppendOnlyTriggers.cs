using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InspectFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AppendOnlyTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Defence in depth for report immutability and audit integrity: the database itself rejects
            // UPDATE/DELETE on finalized report versions, audit logs and tenant responses/observations.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION public.reject_mutation() RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'Table %.% is append-only (% rejected)', TG_TABLE_SCHEMA, TG_TABLE_NAME, TG_OP
                        USING ERRCODE = 'integrity_constraint_violation';
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_report_versions_append_only BEFORE UPDATE OR DELETE ON reports.inspection_report_versions
                    FOR EACH ROW EXECUTE FUNCTION public.reject_mutation();
                CREATE TRIGGER trg_audit_logs_append_only BEFORE UPDATE OR DELETE ON audit.audit_logs
                    FOR EACH ROW EXECUTE FUNCTION public.reject_mutation();
                CREATE TRIGGER trg_tenant_responses_append_only BEFORE UPDATE OR DELETE ON tenants.tenant_responses
                    FOR EACH ROW EXECUTE FUNCTION public.reject_mutation();
                CREATE TRIGGER trg_tenant_observations_append_only BEFORE UPDATE OR DELETE ON tenants.tenant_observations
                    FOR EACH ROW EXECUTE FUNCTION public.reject_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_report_versions_append_only ON reports.inspection_report_versions;
                DROP TRIGGER IF EXISTS trg_audit_logs_append_only ON audit.audit_logs;
                DROP TRIGGER IF EXISTS trg_tenant_responses_append_only ON tenants.tenant_responses;
                DROP TRIGGER IF EXISTS trg_tenant_observations_append_only ON tenants.tenant_observations;
                DROP FUNCTION IF EXISTS public.reject_mutation();
                """);
        }
    }
}
