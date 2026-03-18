using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyHomeSolution.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class budgetintegration01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillBudgetLinks_Budgets_BudgetId",
                table: "BillBudgetLinks");

            migrationBuilder.DropIndex(
                name: "IX_BillBudgetLinks_BillId_BudgetId",
                table: "BillBudgetLinks");

            migrationBuilder.DropColumn(
                name: "SpentAmount",
                table: "BudgetOccurrences");

            migrationBuilder.AddColumn<Guid>(
                name: "BudgetOccurrenceId",
                table: "BillBudgetLinks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "BudgetOccurrences",
                type: "bit",
                nullable: false,
                computedColumnSql: "CAST(CASE WHEN [PeriodStart] <= SYSUTCDATETIME() AND [PeriodEnd] >= SYSUTCDATETIME() THEN 1 ELSE 0 END AS bit)",
                stored: false);

            migrationBuilder.CreateIndex(
                name: "IX_BillBudgetLinks_BillId",
                table: "BillBudgetLinks",
                column: "BillId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillBudgetLinks_BudgetOccurrenceId",
                table: "BillBudgetLinks",
                column: "BudgetOccurrenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillBudgetLinks_BudgetOccurrences_BudgetOccurrenceId",
                table: "BillBudgetLinks",
                column: "BudgetOccurrenceId",
                principalTable: "BudgetOccurrences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BillBudgetLinks_Budgets_BudgetId",
                table: "BillBudgetLinks",
                column: "BudgetId",
                principalTable: "Budgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillBudgetLinks_BudgetOccurrences_BudgetOccurrenceId",
                table: "BillBudgetLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_BillBudgetLinks_Budgets_BudgetId",
                table: "BillBudgetLinks");

            migrationBuilder.DropIndex(
                name: "IX_BillBudgetLinks_BillId",
                table: "BillBudgetLinks");

            migrationBuilder.DropIndex(
                name: "IX_BillBudgetLinks_BudgetOccurrenceId",
                table: "BillBudgetLinks");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "BudgetOccurrences");

            migrationBuilder.DropColumn(
                name: "BudgetOccurrenceId",
                table: "BillBudgetLinks");

            migrationBuilder.AddColumn<decimal>(
                name: "SpentAmount",
                table: "BudgetOccurrences",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_BillBudgetLinks_BillId_BudgetId",
                table: "BillBudgetLinks",
                columns: new[] { "BillId", "BudgetId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BillBudgetLinks_Budgets_BudgetId",
                table: "BillBudgetLinks",
                column: "BudgetId",
                principalTable: "Budgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
