using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication2.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Login = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    Avatar = table.Column<string>(type: "TEXT", nullable: true),
                    Role = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Login);
                });

            migrationBuilder.CreateTable(
                name: "TeacherProfiles",
                columns: table => new
                {
                    UserLogin = table.Column<string>(type: "TEXT", nullable: false),
                    SpecializationCategory = table.Column<string>(type: "TEXT", nullable: false),
                    Experience = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentJob = table.Column<string>(type: "TEXT", nullable: false),
                    TeacherTags = table.Column<string>(type: "TEXT", nullable: true),
                    PortfolioUrl = table.Column<string>(type: "TEXT", nullable: true),
                    About = table.Column<string>(type: "TEXT", nullable: false),
                    ExtraInfo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherProfiles", x => x.UserLogin);
                    table.ForeignKey(
                        name: "FK_TeacherProfiles_Users_UserLogin",
                        column: x => x.UserLogin,
                        principalTable: "Users",
                        principalColumn: "Login",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeacherProfiles");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
