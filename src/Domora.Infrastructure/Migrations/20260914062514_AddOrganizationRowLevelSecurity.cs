using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationRowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            ALTER TABLE "Properties"
            ENABLE ROW LEVEL SECURITY;

            CREATE POLICY "Properties_OrganizationIsolation"
            ON "Properties"
            USING (
                "OrganizationId" =
                 current_setting('app.organization_id', true)::uuid 
            )
            WITH CHECK (
                "OrganizationId" =
                current_setting('app.organization_id', true)::uuid
            );
            """);

            migrationBuilder.Sql("""
            ALTER TABLE "Properties" FORCE ROW LEVEL SECURITY;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            DROP POLICY IF EXISTS "Properties_OrganizationIsolation"
            ON "Properties";

            ALTER TABLE "Properties"
            DISABLE ROW LEVEL SECURITY;
            """);
        }
        
    }
}
