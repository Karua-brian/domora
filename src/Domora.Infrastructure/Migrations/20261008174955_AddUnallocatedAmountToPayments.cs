using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUnallocatedAmountToPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "Payments",
                newName: "UnallocatedCurrency");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "Payments",
                newName: "UnallocatedAmount");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "Payments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TotalCurrency",
                table: "Payments",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TotalCurrency",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "UnallocatedCurrency",
                table: "Payments",
                newName: "Currency");

            migrationBuilder.RenameColumn(
                name: "UnallocatedAmount",
                table: "Payments",
                newName: "Amount");
        }
    }
}
