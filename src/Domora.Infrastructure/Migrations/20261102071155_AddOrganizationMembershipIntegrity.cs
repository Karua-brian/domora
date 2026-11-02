using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationMembershipIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_OrganizationId_Owner",
                table: "OrganizationMemberships",
                column: "OrganizationId",
                unique: true,
                filter: "\"Role\" = 'Owner'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrganizationMemberships_Role",
                table: "OrganizationMemberships",
                sql: "\"Role\" IN ('Owner', 'Member', 'Finance')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationMemberships_OrganizationId_Owner",
                table: "OrganizationMemberships");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrganizationMemberships_Role",
                table: "OrganizationMemberships");

        }
    }
}
