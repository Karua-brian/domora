using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUnitOrganizationIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Units"
                ENABLE ROW LEVEL SECURITY;

                ALTER TABLE "Units"
                FORCE ROW LEVEL SECURITY;

                CREATE POLICY "Units_OrganizationIsolation"
                ON "Units"
                USING (
                    EXISTS (
                        SELECT 1
                        FROM "Properties" p
                        WHERE p."Id" = "Units"."PropertyId"
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
                        FROM "Properties" p
                        WHERE p."Id" = "Units"."PropertyId"
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
                    "Units_OrganizationIsolation"
                ON "Units";

                ALTER TABLE "Units"
                NO FORCE ROW LEVEL SECURITY;

                ALTER TABLE "Units"
                DISABLE ROW LEVEL SECURITY;
            """);
        }
    }
}
