using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyQuizGenerator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAnswerSnapshotFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExplanationSnapshot",
                table: "UserAnswers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HintSnapshot",
                table: "UserAnswers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TypeSnapshot",
                table: "UserAnswers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExplanationSnapshot",
                table: "UserAnswers");

            migrationBuilder.DropColumn(
                name: "HintSnapshot",
                table: "UserAnswers");

            migrationBuilder.DropColumn(
                name: "TypeSnapshot",
                table: "UserAnswers");
        }
    }
}
