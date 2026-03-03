using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication2.Migrations
{
    /// <inheritdoc />
    public partial class course_update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMultipleChoice",
                table: "Steps",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserLogin",
                table: "Comments",
                column: "UserLogin");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Users_UserLogin",
                table: "Comments",
                column: "UserLogin",
                principalTable: "Users",
                principalColumn: "Login",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Users_UserLogin",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_UserLogin",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "IsMultipleChoice",
                table: "Steps");
        }
    }
}
