using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyHomeSolution.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBackgroundServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackgroundServiceDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    QualifiedTypeName = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundServiceDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundServiceLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BackgroundServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResultMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExceptionLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundServiceLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackgroundServiceLogs_BackgroundServiceDefinitions_BackgroundServiceId",
                        column: x => x.BackgroundServiceId,
                        principalTable: "BackgroundServiceDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BackgroundServiceLogs_ExceptionLogs_ExceptionLogId",
                        column: x => x.ExceptionLogId,
                        principalTable: "ExceptionLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundServiceDefinitions_Name",
                table: "BackgroundServiceDefinitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundServiceDefinitions_QualifiedTypeName",
                table: "BackgroundServiceDefinitions",
                column: "QualifiedTypeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundServiceLogs_BackgroundServiceId",
                table: "BackgroundServiceLogs",
                column: "BackgroundServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundServiceLogs_ExceptionLogId",
                table: "BackgroundServiceLogs",
                column: "ExceptionLogId");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundServiceLogs_StartedAt",
                table: "BackgroundServiceLogs",
                column: "StartedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundServiceLogs_Status",
                table: "BackgroundServiceLogs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackgroundServiceLogs");

            migrationBuilder.DropTable(
                name: "BackgroundServiceDefinitions");
        }
    }
}
