using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Middleware.Infra.Migrations
{
    /// <inheritdoc />
    public partial class TransmissionJWS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedAt",
                table: "Invoice",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedJws",
                table: "Invoice",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransmissionId",
                table: "Invoice",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Transmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubmitId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ResponseAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ReferenceNo = table.Column<string>(type: "TEXT", nullable: true),
                    Error = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transmissions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_TransmissionId",
                table: "Invoice",
                column: "TransmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Transmissions_SubmitId",
                table: "Transmissions",
                column: "SubmitId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoice_Transmissions_TransmissionId",
                table: "Invoice",
                column: "TransmissionId",
                principalTable: "Transmissions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoice_Transmissions_TransmissionId",
                table: "Invoice");

            migrationBuilder.DropTable(
                name: "Transmissions");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_TransmissionId",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "SignedAt",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "SignedJws",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "TransmissionId",
                table: "Invoice");
        }
    }
}
