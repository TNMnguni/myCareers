using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myCareers.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EFCoreFixs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Users",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Recruiters",
                newName: "RecruiterId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Applicants",
                newName: "ApplicantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "RecruiterId",
                table: "Recruiters",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ApplicantId",
                table: "Applicants",
                newName: "Id");
        }
    }
}
