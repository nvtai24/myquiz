using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyQuizGenerator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAnswerSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "CorrectAnswersSnapshot",
                table: "UserAnswers",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string[]>(
                name: "OptionsSnapshot",
                table: "UserAnswers",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "QuestionSnapshot",
                table: "UserAnswers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrectAnswersSnapshot",
                table: "UserAnswers");

            migrationBuilder.DropColumn(
                name: "OptionsSnapshot",
                table: "UserAnswers");

            migrationBuilder.DropColumn(
                name: "QuestionSnapshot",
                table: "UserAnswers");
        }
    }
}
