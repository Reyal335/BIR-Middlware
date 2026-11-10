using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Middleware.Infra.Migrations
{
    /// <inheritdoc />
    public partial class InvoiceEisResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "Transmissions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EisResultCode",
                table: "Invoice",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EisResultMessage",
                table: "Invoice",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResultAt",
                table: "Invoice",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Transmissions");

            migrationBuilder.DropColumn(
                name: "EisResultCode",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "EisResultMessage",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "ResultAt",
                table: "Invoice");
        }
    }
}
