using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyHomeSolution.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class exceptionlogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExceptionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExceptionType = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InnerException = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThrownByService = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ClassName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    MethodName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TraceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    IsHandled = table.Column<bool>(type: "bit", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AiAnalysis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiSuggestedPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAiAnalysed = table.Column<bool>(type: "bit", nullable: false),
                    AiAnalysedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExceptionLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionLogs_ExceptionType",
                table: "ExceptionLogs",
                column: "ExceptionType");

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionLogs_IsHandled",
                table: "ExceptionLogs",
                column: "IsHandled");

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionLogs_OccurredAt",
                table: "ExceptionLogs",
                column: "OccurredAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionLogs_Severity",
                table: "ExceptionLogs",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionLogs_ThrownByService",
                table: "ExceptionLogs",
                column: "ThrownByService");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExceptionLogs");
        }
    }
}
