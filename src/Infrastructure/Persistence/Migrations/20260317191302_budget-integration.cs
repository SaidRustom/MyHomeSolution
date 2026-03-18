using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyHomeSolution.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class budgetintegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefaultBudgetId",
                table: "HouseholdTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdTasks_DefaultBudgetId",
                table: "HouseholdTasks",
                column: "DefaultBudgetId");

            migrationBuilder.AddForeignKey(
                name: "FK_HouseholdTasks_Budgets_DefaultBudgetId",
                table: "HouseholdTasks",
                column: "DefaultBudgetId",
                principalTable: "Budgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HouseholdTasks_Budgets_DefaultBudgetId",
                table: "HouseholdTasks");

            migrationBuilder.DropIndex(
                name: "IX_HouseholdTasks_DefaultBudgetId",
                table: "HouseholdTasks");

            migrationBuilder.DropColumn(
                name: "DefaultBudgetId",
                table: "HouseholdTasks");
        }
    }
}
