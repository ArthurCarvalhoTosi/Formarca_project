using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Formarca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTableViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserTableViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TableKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ColumnKeysJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTableViews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserTableViews_UserId_TableKey",
                table: "UserTableViews",
                columns: new[] { "UserId", "TableKey" });

            migrationBuilder.CreateIndex(
                name: "IX_UserTableViews_UserId_TableKey_Name",
                table: "UserTableViews",
                columns: new[] { "UserId", "TableKey", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserTableViews");
        }
    }
}
