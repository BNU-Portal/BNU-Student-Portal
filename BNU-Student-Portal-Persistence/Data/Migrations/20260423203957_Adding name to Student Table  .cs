using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BNU_Student_Portal_Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingnametoStudentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Students",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Students");
        }
    }
}
