using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Formarca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrademarkStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TrademarkStatusChangedAtUtc",
                table: "Processes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TrademarkStatusId",
                table: "Processes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TrademarkStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Phase = table.Column<int>(type: "int", nullable: false),
                    Tone = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrademarkStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessStatusChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessStatusChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessStatusChanges_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessStatusChanges_TrademarkStatuses_FromStatusId",
                        column: x => x.FromStatusId,
                        principalTable: "TrademarkStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessStatusChanges_TrademarkStatuses_ToStatusId",
                        column: x => x.ToStatusId,
                        principalTable: "TrademarkStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Processes_TrademarkStatusId",
                table: "Processes",
                column: "TrademarkStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessStatusChanges_FromStatusId",
                table: "ProcessStatusChanges",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessStatusChanges_ProcessId",
                table: "ProcessStatusChanges",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessStatusChanges_TenantId",
                table: "ProcessStatusChanges",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessStatusChanges_ToStatusId",
                table: "ProcessStatusChanges",
                column: "ToStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_TrademarkStatuses_Code",
                table: "TrademarkStatuses",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TrademarkStatuses_SortOrder",
                table: "TrademarkStatuses",
                column: "SortOrder");

            migrationBuilder.AddForeignKey(
                name: "FK_Processes_TrademarkStatuses_TrademarkStatusId",
                table: "Processes",
                column: "TrademarkStatusId",
                principalTable: "TrademarkStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Processes_TrademarkStatuses_TrademarkStatusId",
                table: "Processes");

            migrationBuilder.DropTable(
                name: "ProcessStatusChanges");

            migrationBuilder.DropTable(
                name: "TrademarkStatuses");

            migrationBuilder.DropIndex(
                name: "IX_Processes_TrademarkStatusId",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "TrademarkStatusChangedAtUtc",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "TrademarkStatusId",
                table: "Processes");
        }
    }
}
