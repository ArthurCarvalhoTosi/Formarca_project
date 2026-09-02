using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Formarca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BodyMarkdown",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Contracts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "ClientSignaturePng",
                table: "Contracts",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Contracts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MinimumTerm",
                table: "Contracts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "12 (doze) meses");

            migrationBuilder.AddColumn<string>(
                name: "Number",
                table: "Contracts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Contracts",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Contracts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TemplateKey",
                table: "Contracts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "prestacao-servicos");

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "Contracts",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ContractFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractFiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractFiles_ContractId",
                table: "ContractFiles",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractFiles_ContractId_Kind",
                table: "ContractFiles",
                columns: new[] { "ContractId", "Kind" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ContractFiles_TenantId",
                table: "ContractFiles",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractFiles");

            migrationBuilder.DropColumn(
                name: "BodyMarkdown",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ClientSignaturePng",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "MinimumTerm",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "TemplateKey",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "Contracts");
        }
    }
}
