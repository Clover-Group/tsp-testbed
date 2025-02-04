using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TspTestbed.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "tests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "sources",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "sinks",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name",
                table: "tests");

            migrationBuilder.DropColumn(
                name: "name",
                table: "sources");

            migrationBuilder.DropColumn(
                name: "name",
                table: "sinks");
        }
    }
}
