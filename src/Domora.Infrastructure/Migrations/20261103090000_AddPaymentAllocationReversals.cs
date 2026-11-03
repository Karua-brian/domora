using Domora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domora.Infrastructure.Migrations;

[DbContext(typeof(DomoraDbContext))]
[Migration("20261103090000_AddPaymentAllocationReversals")]
public partial class AddPaymentAllocationReversals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddUniqueConstraint(
            name: "AK_PaymentAllocations_OrganizationId_Id",
            table: "PaymentAllocations",
            columns: new[] { "OrganizationId", "Id" });

        migrationBuilder.CreateTable(
            name: "PaymentAllocationReversals",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                PaymentAllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                ReversedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentAllocationReversals", x => x.Id);
                table.ForeignKey("FK_PaymentAllocationReversals_PaymentAllocations_OrganizationId_PaymentAllocationId",
                    x => new { x.OrganizationId, x.PaymentAllocationId }, "PaymentAllocations",
                    new[] { "OrganizationId", "Id" }, onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_PaymentAllocationReversals_Users_ActorUserId",
                    x => x.ActorUserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_PaymentAllocationReversals_OrganizationId_PaymentAllocationId",
            table: "PaymentAllocationReversals", columns: new[] { "OrganizationId", "PaymentAllocationId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_PaymentAllocationReversals_ActorUserId",
            table: "PaymentAllocationReversals", column: "ActorUserId");

        migrationBuilder.Sql("""
            ALTER TABLE "PaymentAllocationReversals" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "PaymentAllocationReversals" FORCE ROW LEVEL SECURITY;
            CREATE POLICY "PaymentAllocationReversals_OrganizationIsolation"
            ON "PaymentAllocationReversals"
            USING ("OrganizationId" = NULLIF(current_setting('app.organization_id', true), '')::uuid)
            WITH CHECK ("OrganizationId" = NULLIF(current_setting('app.organization_id', true), '')::uuid);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP POLICY IF EXISTS "PaymentAllocationReversals_OrganizationIsolation" ON "PaymentAllocationReversals";
            ALTER TABLE "PaymentAllocationReversals" NO FORCE ROW LEVEL SECURITY;
            ALTER TABLE "PaymentAllocationReversals" DISABLE ROW LEVEL SECURITY;
            """);
        migrationBuilder.DropTable(name: "PaymentAllocationReversals");
        migrationBuilder.DropUniqueConstraint(name: "AK_PaymentAllocations_OrganizationId_Id", table: "PaymentAllocations");
    }
}
