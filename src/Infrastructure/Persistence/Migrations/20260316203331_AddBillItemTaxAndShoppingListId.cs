using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyHomeSolution.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBillItemTaxAndShoppingListId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTaxable",
                table: "BillItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ShoppingListId",
                table: "BillItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "BillItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_BillItems_ShoppingListId",
                table: "BillItems",
                column: "ShoppingListId",
                filter: "ShoppingListId IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BillItems_ShoppingListId",
                table: "BillItems");

            migrationBuilder.DropColumn(
                name: "IsTaxable",
                table: "BillItems");

            migrationBuilder.DropColumn(
                name: "ShoppingListId",
                table: "BillItems");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "BillItems");
        }
    }
}
