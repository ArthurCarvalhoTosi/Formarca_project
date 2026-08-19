using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Formarca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowDuplicateTenantDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_Document",
                table: "Tenants");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Document",
                table: "Tenants",
                column: "Document");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_Document",
                table: "Tenants");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Document",
                table: "Tenants",
                column: "Document",
                unique: true,
                filter: "[Document] IS NOT NULL AND [IsDeleted] = 0");
        }
    }
}
