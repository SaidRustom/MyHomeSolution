using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyHomeSolution.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class budgetmodule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefaultBudgetId",
                table: "ShoppingLists",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillRelatedItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RelatedEntityName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillRelatedItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillRelatedItems_Bills_BillId",
                        column: x => x.BillId,
                        principalTable: "Bills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Budgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    ParentBudgetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Budgets_Budgets_ParentBudgetId",
                        column: x => x.ParentBudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillBudgetLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BudgetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillBudgetLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillBudgetLinks_Bills_BillId",
                        column: x => x.BillId,
                        principalTable: "Bills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillBudgetLinks_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BudgetOccurrences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BudgetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SpentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CarryoverAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetOccurrences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetOccurrences_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BudgetTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceOccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationOccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetTransfers_BudgetOccurrences_DestinationOccurrenceId",
                        column: x => x.DestinationOccurrenceId,
                        principalTable: "BudgetOccurrences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetTransfers_BudgetOccurrences_SourceOccurrenceId",
                        column: x => x.SourceOccurrenceId,
                        principalTable: "BudgetOccurrences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingLists_DefaultBudgetId",
                table: "ShoppingLists",
                column: "DefaultBudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_BillBudgetLinks_BillId_BudgetId",
                table: "BillBudgetLinks",
                columns: new[] { "BillId", "BudgetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillBudgetLinks_BudgetId",
                table: "BillBudgetLinks",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_BillRelatedItems_BillId",
                table: "BillRelatedItems",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_BillRelatedItems_RelatedEntityType_RelatedEntityId",
                table: "BillRelatedItems",
                columns: new[] { "RelatedEntityType", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetOccurrences_BudgetId",
                table: "BudgetOccurrences",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetOccurrences_PeriodStart_PeriodEnd",
                table: "BudgetOccurrences",
                columns: new[] { "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_Category",
                table: "Budgets",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_IsDeleted",
                table: "Budgets",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_ParentBudgetId",
                table: "Budgets",
                column: "ParentBudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_Period",
                table: "Budgets",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_StartDate",
                table: "Budgets",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransfers_DestinationOccurrenceId",
                table: "BudgetTransfers",
                column: "DestinationOccurrenceId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransfers_SourceOccurrenceId",
                table: "BudgetTransfers",
                column: "SourceOccurrenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingLists_Budgets_DefaultBudgetId",
                table: "ShoppingLists",
                column: "DefaultBudgetId",
                principalTable: "Budgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingLists_Budgets_DefaultBudgetId",
                table: "ShoppingLists");

            migrationBuilder.DropTable(
                name: "BillBudgetLinks");

            migrationBuilder.DropTable(
                name: "BillRelatedItems");

            migrationBuilder.DropTable(
                name: "BudgetTransfers");

            migrationBuilder.DropTable(
                name: "BudgetOccurrences");

            migrationBuilder.DropTable(
                name: "Budgets");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingLists_DefaultBudgetId",
                table: "ShoppingLists");

            migrationBuilder.DropColumn(
                name: "DefaultBudgetId",
                table: "ShoppingLists");
        }
    }
}
