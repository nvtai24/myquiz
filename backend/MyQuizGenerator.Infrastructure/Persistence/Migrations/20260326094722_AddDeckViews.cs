using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyQuizGenerator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeckViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeckViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeckId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeckViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeckViews_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeckViews_Decks_DeckId",
                        column: x => x.DeckId,
                        principalTable: "Decks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeckViews_DeckId_UserId",
                table: "DeckViews",
                columns: new[] { "DeckId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeckViews_UserId",
                table: "DeckViews",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeckViews");
        }
    }
}
