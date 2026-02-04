using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyQuizGenerator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRefreshTokenColumnNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExpiryDate",
                table: "RefreshTokens",
                newName: "ExpiryAt");

            migrationBuilder.RenameColumn(
                name: "CreationDate",
                table: "RefreshTokens",
                newName: "CreationAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExpiryAt",
                table: "RefreshTokens",
                newName: "ExpiryDate");

            migrationBuilder.RenameColumn(
                name: "CreationAt",
                table: "RefreshTokens",
                newName: "CreationDate");
        }
    }
}
