using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication2.Migrations
{
    /// <inheritdoc />
    public partial class updatecoursemodellogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrectTextAnswer",
                table: "Steps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsManualCheck",
                table: "Steps",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxPoints",
                table: "Steps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StepSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserLogin = table.Column<string>(type: "TEXT", nullable: false),
                    StepId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserAnswerText = table.Column<string>(type: "TEXT", nullable: true),
                    IsPending = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    EarnedPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    TeacherComment = table.Column<string>(type: "TEXT", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StepSubmissions_Steps_StepId",
                        column: x => x.StepId,
                        principalTable: "Steps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_AuthorLogin",
                table: "Courses",
                column: "AuthorLogin");

            migrationBuilder.CreateIndex(
                name: "IX_StepSubmissions_StepId",
                table: "StepSubmissions",
                column: "StepId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_TeacherProfiles_AuthorLogin",
                table: "Courses",
                column: "AuthorLogin",
                principalTable: "TeacherProfiles",
                principalColumn: "UserLogin",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_TeacherProfiles_AuthorLogin",
                table: "Courses");

            migrationBuilder.DropTable(
                name: "StepSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_Courses_AuthorLogin",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CorrectTextAnswer",
                table: "Steps");

            migrationBuilder.DropColumn(
                name: "IsManualCheck",
                table: "Steps");

            migrationBuilder.DropColumn(
                name: "MaxPoints",
                table: "Steps");
        }
    }
}
