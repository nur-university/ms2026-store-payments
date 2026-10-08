using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCashierCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Invoice numbers (F-00000001, ...) come from this sequence so they stay unique and consecutive.
            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS invoice_number_seq START WITH 1 INCREMENT BY 1;");

            migrationBuilder.AddColumn<decimal>(
                name: "amountReceived",
                table: "payment",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "changeGiven",
                table: "payment",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "collectedBy",
                table: "payment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customerDocument",
                table: "payment",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customerName",
                table: "payment",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invoiceNumber",
                table: "payment",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "method",
                table: "payment",
                type: "character varying(25)",
                maxLength: 25,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference",
                table: "payment",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_invoiceNumber",
                table: "payment",
                column: "invoiceNumber",
                unique: true,
                filter: "\"invoiceNumber\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS invoice_number_seq;");

            migrationBuilder.DropIndex(
                name: "IX_payment_invoiceNumber",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "amountReceived",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "changeGiven",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "collectedBy",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "customerDocument",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "customerName",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "invoiceNumber",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "method",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "reference",
                table: "payment");
        }
    }
}
