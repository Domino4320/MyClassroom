using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication2.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseBookmarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseBookmarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserLogin = table.Column<string>(type: "TEXT", nullable: false),
                    CourseId = table.Column<int>(type: "INTEGER", nullable: false),
                    StepId = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseBookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseBookmarks_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseBookmarks_Steps_StepId",
                        column: x => x.StepId,
                        principalTable: "Steps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseBookmarks_CourseId",
                table: "CourseBookmarks",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseBookmarks_StepId",
                table: "CourseBookmarks",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseBookmarks_UserLogin_CourseId",
                table: "CourseBookmarks",
                columns: new[] { "UserLogin", "CourseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseBookmarks");
        }
    }
}
