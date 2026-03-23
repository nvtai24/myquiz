using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyQuizGenerator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDeckDocumentUrlsToSingle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentUrls",
                table: "Decks");

            migrationBuilder.AddColumn<string>(
                name: "DocumentUrl",
                table: "Decks",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentUrl",
                table: "Decks");

            migrationBuilder.AddColumn<string[]>(
                name: "DocumentUrls",
                table: "Decks",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);
        }
    }
}
