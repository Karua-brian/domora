using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Domora.Infrastructure.Persistence;

#nullable disable

namespace Domora.Infrastructure.Migrations;

[DbContext(typeof(DomoraDbContext))]
[Migration("20261030120000_AddPaymentVoids")]
public partial class AddPaymentVoids : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PaymentVoids",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                VoidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentVoids", x => x.Id);
                table.ForeignKey(
                    name: "FK_PaymentVoids_Payments_OrganizationId_PaymentId",
                    columns: x => new { x.OrganizationId, x.PaymentId },
                    principalTable: "Payments",
                    principalColumns: new[] { "OrganizationId", "Id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PaymentVoids_Users_ActorUserId",
                    column: x => x.ActorUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_PaymentVoids_OrganizationId_PaymentId",
            table: "PaymentVoids",
            columns: new[] { "OrganizationId", "PaymentId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PaymentVoids_ActorUserId",
            table: "PaymentVoids",
            column: "ActorUserId");

        migrationBuilder.Sql("""
            ALTER TABLE "PaymentVoids" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "PaymentVoids" FORCE ROW LEVEL SECURITY;
            CREATE POLICY "PaymentVoids_OrganizationIsolation"
            ON "PaymentVoids"
            USING (
                "OrganizationId" =
                NULLIF(current_setting('app.organization_id', true), '')::uuid
            )
            WITH CHECK (
                "OrganizationId" =
                NULLIF(current_setting('app.organization_id', true), '')::uuid
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP POLICY IF EXISTS "PaymentVoids_OrganizationIsolation" ON "PaymentVoids";
            ALTER TABLE "PaymentVoids" NO FORCE ROW LEVEL SECURITY;
            ALTER TABLE "PaymentVoids" DISABLE ROW LEVEL SECURITY;
            """);
        migrationBuilder.DropTable(name: "PaymentVoids");
    }
}
