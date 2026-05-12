using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication2.Migrations
{
    /// <inheritdoc />
    public partial class SyncTeacherReviewsTable : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// Миграция <c>20260512063445_teachers</c> имеет пустой Up(), хотя модель уже содержит TeacherReviews —
        /// таблица в SQLite не была создана. Эта миграция создаёт таблицу (при уже раннем применении teachers без DDL).
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeacherReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeacherLogin = table.Column<string>(type: "TEXT", nullable: false),
                    UserLogin = table.Column<string>(type: "TEXT", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRecommended = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherReviews_TeacherProfiles_TeacherLogin",
                        column: x => x.TeacherLogin,
                        principalTable: "TeacherProfiles",
                        principalColumn: "UserLogin",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherReviews_Users_UserLogin",
                        column: x => x.UserLogin,
                        principalTable: "Users",
                        principalColumn: "Login",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherReviews_TeacherLogin",
                table: "TeacherReviews",
                column: "TeacherLogin");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherReviews_UserLogin",
                table: "TeacherReviews",
                column: "UserLogin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TeacherReviews");
        }
    }
}
