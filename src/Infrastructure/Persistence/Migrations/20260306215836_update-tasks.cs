using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyHomeSolution.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class updatetasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BillId",
                table: "TaskOccurrences",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoCreateBill",
                table: "HouseholdTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultBillAmount",
                table: "HouseholdTasks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultBillCategory",
                table: "HouseholdTasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultBillCurrency",
                table: "HouseholdTasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultBillTitle",
                table: "HouseholdTasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskOccurrences_BillId",
                table: "TaskOccurrences",
                column: "BillId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskOccurrences_Bills_BillId",
                table: "TaskOccurrences",
                column: "BillId",
                principalTable: "Bills",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskOccurrences_Bills_BillId",
                table: "TaskOccurrences");

            migrationBuilder.DropIndex(
                name: "IX_TaskOccurrences_BillId",
                table: "TaskOccurrences");

            migrationBuilder.DropColumn(
                name: "BillId",
                table: "TaskOccurrences");

            migrationBuilder.DropColumn(
                name: "AutoCreateBill",
                table: "HouseholdTasks");

            migrationBuilder.DropColumn(
                name: "DefaultBillAmount",
                table: "HouseholdTasks");

            migrationBuilder.DropColumn(
                name: "DefaultBillCategory",
                table: "HouseholdTasks");

            migrationBuilder.DropColumn(
                name: "DefaultBillCurrency",
                table: "HouseholdTasks");

            migrationBuilder.DropColumn(
                name: "DefaultBillTitle",
                table: "HouseholdTasks");
        }
    }
}
