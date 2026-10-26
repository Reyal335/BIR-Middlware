using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Middleware.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddLineTaxBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NonVatSales",
                table: "InvoiceLines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ScPwdDiscount",
                table: "InvoiceLines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                table: "InvoiceLines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatExemptSales",
                table: "InvoiceLines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatableSales",
                table: "InvoiceLines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ZeroRatedSales",
                table: "InvoiceLines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Totals_NonVatSales",
                table: "Invoice",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NonVatSales",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "ScPwdDiscount",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "VatExemptSales",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "VatableSales",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "ZeroRatedSales",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "Totals_NonVatSales",
                table: "Invoice");
        }
    }
}
