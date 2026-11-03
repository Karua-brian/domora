using Domora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domora.Infrastructure.Migrations;

[DbContext(typeof(DomoraDbContext))]
[Migration("20261103130000_AddInvoiceVoids")]
public partial class AddInvoiceVoids : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "InvoiceVoids",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                ReplacementInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                VoidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InvoiceVoids", x => x.Id);
                table.ForeignKey("FK_InvoiceVoids_Invoices_OrganizationId_InvoiceId",
                    x => new { x.OrganizationId, x.InvoiceId }, "Invoices",
                    new[] { "OrganizationId", "Id" }, onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_InvoiceVoids_Invoices_OrganizationId_ReplacementInvoiceId",
                    x => new { x.OrganizationId, x.ReplacementInvoiceId }, "Invoices",
                    new[] { "OrganizationId", "Id" }, onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_InvoiceVoids_Users_ActorUserId",
                    x => x.ActorUserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "UX_InvoiceVoids_OrganizationId_InvoiceId",
            table: "InvoiceVoids", columns: new[] { "OrganizationId", "InvoiceId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_InvoiceVoids_ActorUserId", table: "InvoiceVoids", column: "ActorUserId");
        migrationBuilder.CreateIndex(name: "UX_InvoiceVoids_OrganizationId_ReplacementInvoiceId",
            table: "InvoiceVoids", columns: new[] { "OrganizationId", "ReplacementInvoiceId" }, unique: true,
            filter: "\"ReplacementInvoiceId\" IS NOT NULL");

        migrationBuilder.Sql("""
            ALTER TABLE "InvoiceVoids" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "InvoiceVoids" FORCE ROW LEVEL SECURITY;
            CREATE POLICY "InvoiceVoids_OrganizationIsolation"
            ON "InvoiceVoids"
            USING ("OrganizationId" = NULLIF(current_setting('app.organization_id', true), '')::uuid)
            WITH CHECK ("OrganizationId" = NULLIF(current_setting('app.organization_id', true), '')::uuid);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP POLICY IF EXISTS "InvoiceVoids_OrganizationIsolation" ON "InvoiceVoids";
            ALTER TABLE "InvoiceVoids" NO FORCE ROW LEVEL SECURITY;
            ALTER TABLE "InvoiceVoids" DISABLE ROW LEVEL SECURITY;
            """);
        migrationBuilder.DropTable(name: "InvoiceVoids");
    }
}
