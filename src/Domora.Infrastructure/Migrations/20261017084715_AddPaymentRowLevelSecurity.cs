using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentRowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Payments" 
                ENABLE ROW LEVEL SECURITY;

                CREATE POLICY "Payments_OrganizationIsolation"
                ON "Payments"
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
                ALTER TABLE "Payments"
                FORCE ROW LEVEL SECURITY;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            DROP POLICY IF EXISTS "Payments_OrganizationIsolation"
            ON "Payments";

            ALTER TABLE "Payments" 
                NO FORCE ROW LEVEL SECURITY;

            ALTER TABLE "Payments"
            DISABLE ROW LEVEL SECURITY;
            """);
        }
    }
}