using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication2.Migrations
{
    /// <inheritdoc />
    public partial class AddForum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ForumDiscussions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    AuthorLogin = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumDiscussions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForumDiscussions_Users_AuthorLogin",
                        column: x => x.AuthorLogin,
                        principalTable: "Users",
                        principalColumn: "Login",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ForumMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DiscussionId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserLogin = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 3000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ParentMessageId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForumMessages_ForumDiscussions_DiscussionId",
                        column: x => x.DiscussionId,
                        principalTable: "ForumDiscussions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ForumMessages_ForumMessages_ParentMessageId",
                        column: x => x.ParentMessageId,
                        principalTable: "ForumMessages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ForumMessages_Users_UserLogin",
                        column: x => x.UserLogin,
                        principalTable: "Users",
                        principalColumn: "Login",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForumDiscussions_AuthorLogin",
                table: "ForumDiscussions",
                column: "AuthorLogin");

            migrationBuilder.CreateIndex(
                name: "IX_ForumMessages_DiscussionId",
                table: "ForumMessages",
                column: "DiscussionId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumMessages_ParentMessageId",
                table: "ForumMessages",
                column: "ParentMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumMessages_UserLogin",
                table: "ForumMessages",
                column: "UserLogin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForumMessages");

            migrationBuilder.DropTable(
                name: "ForumDiscussions");
        }
    }
}

