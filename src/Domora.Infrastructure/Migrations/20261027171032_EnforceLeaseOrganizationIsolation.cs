using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceLeaseOrganizationIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Leases"
                ENABLE ROW LEVEL SECURITY;

                ALTER TABLE "Leases"
                FORCE ROW LEVEL SECURITY;

                CREATE POLICY "Leases_OrganizationIsolation"
                ON "Leases"
                USING (
                    EXISTS (
                        SELECT 1
                        FROM "Units" u
                        JOIN "Properties" p
                            ON p."Id" = u."PropertyId"
                        WHERE u."Id" = "Leases"."UnitId"
                        AND p."OrganizationId" =
                            NULLIF(
                                current_setting(
                                    'app.organization_id',
                                    true
                                ),
                                ''
                        )::uuid
                    )
                )
                WITH CHECK (
                    EXISTS (
                        SELECT 1
                        FROM "Units" u
                        JOIN "Properties" p
                            ON p."Id" = u."PropertyId"
                        WHERE u."Id" = "Leases"."UnitId"
                        AND p."OrganizationId" =
                            NULLIF(
                                current_setting(
                                    'app.organization_id',
                                    true
                                ),
                                ''
                        )::uuid
                    )
                );
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS
                    "Leases_OrganizationIsolation"
                ON "Leases";

                ALTER TABLE "Leases"
                NO FORCE ROW LEVEL SECURITY;

                ALTER TABLE "Leases"
                DISABLE ROW LEVEL SECURITY;
            """);
        }
    }
}
