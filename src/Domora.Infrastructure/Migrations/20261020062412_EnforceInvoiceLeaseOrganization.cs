using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceInvoiceLeaseOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION 
                    enforce_invoice_lease_organization()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    lease_organization_id uuid;
                BEGIN
                    SELECT p."OrganizationId"
                    INTO lease_organization_id
                    FROM "Leases" l
                    JOIN "Units" u
                        ON u."Id" = l."UnitId"       
                    JOIN "Properties" p
                        ON p."Id" = u."PropertyId"    
                    WHERE l."Id" = NEW."LeaseId";     

                    IF lease_organization_id is NULL
                        OR lease_organization_id <> NEW."OrganizationId"
                    THEN
                        RAISE EXCEPTION
                            'Invoice organization must match lease organization'
                            USING ERRCODE = '23514';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER
                    "TR_Invoices_LeaseOrganization"
                BEFORE INSERT OR UPDATE OF "LeaseId", "OrganizationId"
                ON "Invoices"
                FOR EACH ROW
                EXECUTE FUNCTION
                    enforce_invoice_lease_organization();
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS
                    "TR_Invoices_LeaseOrganization"
                ON "Invoices";

                DROP FUNCTION IF EXISTS
                    enforce_invoice_lease_organization();
            """);
        }
    }
}
