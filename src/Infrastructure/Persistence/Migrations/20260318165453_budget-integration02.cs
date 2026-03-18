using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyHomeSolution.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class budgetintegration02 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE FUNCTION dbo.fn_GetSpentAmount (@BudgetOccurrenceId uniqueidentifier)
                RETURNS DECIMAL(18, 2)
                AS
                BEGIN
                    DECLARE @Result DECIMAL(18, 2);
                    SELECT @Result = ISNULL(SUM(b.[Amount]), 0)
                    FROM [Bills] b
                    INNER JOIN [BillBudgetLinks] bbl ON bbl.[BillId] = b.[Id]
                    WHERE bbl.[BudgetOccurrenceId] = @BudgetOccurrenceId AND b.[IsDeleted] = 0;
                    RETURN @Result;
                END");

            migrationBuilder.AddColumn<decimal>(
                name: "Balance",
                table: "BudgetOccurrences",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                computedColumnSql: "[AllocatedAmount] + [CarryoverAmount] - dbo.fn_GetSpentAmount([Id])",
                stored: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SpentAmount",
                table: "BudgetOccurrences",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                computedColumnSql: "dbo.fn_GetSpentAmount([Id])",
                stored: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Balance",
                table: "BudgetOccurrences");

            migrationBuilder.DropColumn(
                name: "SpentAmount",
                table: "BudgetOccurrences");
        }
    }
}
