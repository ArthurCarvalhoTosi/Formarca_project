using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Formarca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAutentiqueContractFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AutentiqueDocumentId",
                table: "Contracts",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutentiqueSignUrl",
                table: "Contracts",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutentiqueSignerPublicId",
                table: "Contracts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutentiqueStatus",
                table: "Contracts",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_AutentiqueDocumentId",
                table: "Contracts",
                column: "AutentiqueDocumentId",
                unique: true,
                filter: "[AutentiqueDocumentId] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contracts_AutentiqueDocumentId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "AutentiqueDocumentId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "AutentiqueSignUrl",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "AutentiqueSignerPublicId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "AutentiqueStatus",
                table: "Contracts");
        }
    }
}
