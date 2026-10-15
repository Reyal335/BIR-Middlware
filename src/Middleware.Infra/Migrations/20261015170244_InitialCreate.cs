using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Middleware.Infra.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invoice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StoreCode = table.Column<string>(type: "TEXT", nullable: false),
                    PosTerminalId = table.Column<string>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<string>(type: "TEXT", nullable: false),
                    InvoiceType = table.Column<string>(type: "TEXT", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "TEXT", nullable: false),
                    TransactionDateTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Cashier = table.Column<string>(type: "TEXT", nullable: false),
                    Seller_RegisteredName = table.Column<string>(type: "TEXT", nullable: false),
                    Seller_TradeName = table.Column<string>(type: "TEXT", nullable: false),
                    Seller_Tin = table.Column<string>(type: "TEXT", nullable: false),
                    Seller_BranchCode = table.Column<string>(type: "TEXT", nullable: false),
                    Seller_Address = table.Column<string>(type: "TEXT", nullable: false),
                    Seller_VatRegistration = table.Column<string>(type: "TEXT", nullable: false),
                    Seller_PtuOrAccreditationNo = table.Column<string>(type: "TEXT", nullable: false),
                    Seller_MachineAccreditationNo = table.Column<string>(type: "TEXT", nullable: false),
                    Customer_Type = table.Column<string>(type: "TEXT", nullable: false),
                    Customer_RegisteredName = table.Column<string>(type: "TEXT", nullable: true),
                    Customer_Tin = table.Column<string>(type: "TEXT", nullable: true),
                    Customer_Address = table.Column<string>(type: "TEXT", nullable: true),
                    Totals_GrossAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Totals_VatableSales = table.Column<decimal>(type: "TEXT", nullable: false),
                    Totals_VatExemptSales = table.Column<decimal>(type: "TEXT", nullable: false),
                    Totals_ZeroRatedSales = table.Column<decimal>(type: "TEXT", nullable: false),
                    Totals_VatAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Totals_ScPwdDiscount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Totals_OtherDiscount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Totals_TotalDue = table.Column<decimal>(type: "TEXT", nullable: false),
                    Payment_Method = table.Column<string>(type: "TEXT", nullable: false),
                    Payment_AmountTendered = table.Column<decimal>(type: "TEXT", nullable: false),
                    Payment_Change = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    EisUniqueId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoice", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LineNo = table.Column<int>(type: "INTEGER", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    LineAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TaxClass = table.Column<string>(type: "TEXT", nullable: false),
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoice_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_StoreCode_PosTerminalId_InvoiceNumber",
                table: "Invoice",
                columns: new[] { "StoreCode", "PosTerminalId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId",
                table: "InvoiceLines",
                column: "InvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "Invoice");
        }
    }
}
