using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationIntegrityToPaymentAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "PaymentAllocations"
                FORCE ROW LEVEL SECURITY;
            """);

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentAllocations_Invoices_InvoiceId",
                table: "PaymentAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentAllocations_Payments_PaymentId",
                table: "PaymentAllocations");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAllocations_InvoiceId",
                table: "PaymentAllocations");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAllocations_PaymentId",
                table: "PaymentAllocations");

            migrationBuilder.DropIndex(
                name: "UX_Invoices_OrganizationId_Id",
                table: "Invoices");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "PaymentAllocations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Payments_OrganizationId_Id",
                table: "Payments",
                columns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Invoices_OrganizationId_Id",
                table: "Invoices",
                columns: new[] { "OrganizationId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_OrganizationId_InvoiceId",
                table: "PaymentAllocations",
                columns: new[] { "OrganizationId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_OrganizationId_PaymentId",
                table: "PaymentAllocations",
                columns: new[] { "OrganizationId", "PaymentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAllocations_Invoices_OrganizationId_InvoiceId",
                table: "PaymentAllocations",
                columns: new[] { "OrganizationId", "InvoiceId" },
                principalTable: "Invoices",
                principalColumns: new[] { "OrganizationId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAllocations_Payments_OrganizationId_PaymentId",
                table: "PaymentAllocations",
                columns: new[] { "OrganizationId", "PaymentId" },
                principalTable: "Payments",
                principalColumns: new[] { "OrganizationId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                ALTER TABLE "PaymentAllocations"
                ENABLE ROW LEVEL SECURITY;

                CREATE POLICY "PaymentAllocations_OrganizationIsolation"
                    ON "PaymentAllocations"
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentAllocations_Invoices_OrganizationId_InvoiceId",
                table: "PaymentAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentAllocations_Payments_OrganizationId_PaymentId",
                table: "PaymentAllocations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Payments_OrganizationId_Id",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAllocations_OrganizationId_InvoiceId",
                table: "PaymentAllocations");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAllocations_OrganizationId_PaymentId",
                table: "PaymentAllocations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Invoices_OrganizationId_Id",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "PaymentAllocations");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_InvoiceId",
                table: "PaymentAllocations",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_PaymentId",
                table: "PaymentAllocations",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "UX_Invoices_OrganizationId_Id",
                table: "Invoices",
                columns: new[] { "OrganizationId", "Id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAllocations_Invoices_InvoiceId",
                table: "PaymentAllocations",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAllocations_Payments_PaymentId",
                table: "PaymentAllocations",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
            DROP POLICY IF EXISTS "PaymentAllocations_OrganizationIsolation"
            ON "PaymentAllocations";

            ALTER TABLE "PaymentAllocations"
            NO FORCE ROW LEVEL SECURITY;

            ALTER TABLE "PaymentAllocations"
            DISABLE ROW LEVEL SECURITY;
            """);
        }
    }
}
