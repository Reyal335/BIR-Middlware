using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Middleware.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddEisControlCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TaxClass",
                table: "InvoiceLines",
                newName: "TaxType");

            migrationBuilder.CreateTable(
                name: "EisControlCounters",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    LastValue = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateIndex(
                name: "IX_EisControlCounters_LastValue",
                table: "EisControlCounters",
                column: "LastValue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EisControlCounters");

            migrationBuilder.RenameColumn(
                name: "TaxType",
                table: "InvoiceLines",
                newName: "TaxClass");
        }
    }
}
