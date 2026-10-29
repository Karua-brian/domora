using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicesRowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Invoices" 
                ENABLE ROW LEVEL SECURITY;

                CREATE POLICY "Invoices_OrganizationIsolation"
                ON "Invoices"
                USING (
                    "OrganizationId" =
                    NULLIF(current_setting('app.organization_id', true), '')::uuid
                )
                WITH CHECK (
                    "OrganizationId" =
                    NULLIF(current_setting('app.organization_id', true), '')::uuid
                );
            """);

            migrationBuilder.Sql("""
                ALTER TABLE "Invoices" 
                FORCE ROW LEVEL SECURITY;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            DROP POLICY IF EXISTS "Invoices_OrganizationIsolation"
            ON "Invoices";

            ALTER TABLE "Invoices"
            NO FORCE ROW LEVEL SECURITY;

            ALTER TABLE "Invoices"
            DISABLE ROW LEVEL SECURITY;
            """);
        }
    }
}