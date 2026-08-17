using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaseLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leases_UnitId",
                table: "Leases");

            migrationBuilder.RenameColumn(
                name: "MonthlyRentCurrency",
                table: "Leases",
                newName: "Currency");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "Leases",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Leases",
                type: "text",
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_Leases_UnitId",
                table: "Leases",
                column: "UnitId",
                unique: true,
                filter: "\"Status\" = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leases_UnitId",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Leases");

            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "Leases",
                newName: "MonthlyRentCurrency");

            migrationBuilder.CreateIndex(
                name: "IX_Leases_UnitId",
                table: "Leases",
                column: "UnitId");
        }
    }
}
