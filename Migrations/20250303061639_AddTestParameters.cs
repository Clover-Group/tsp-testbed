using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TspTestbed.Migrations
{
    /// <inheritdoc />
    public partial class AddTestParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "chunk_size_ms",
                table: "tests",
                type: "bigint",
                nullable: false,
                defaultValue: 900000L);

            migrationBuilder.AddColumn<long>(
                name: "default_events_gap_ms",
                table: "tests",
                type: "bigint",
                nullable: false,
                defaultValue: 2000L);

            migrationBuilder.AddColumn<long>(
                name: "events_max_gap_ms",
                table: "tests",
                type: "bigint",
                nullable: false,
                defaultValue: 60000L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "chunk_size_ms",
                table: "tests");

            migrationBuilder.DropColumn(
                name: "default_events_gap_ms",
                table: "tests");

            migrationBuilder.DropColumn(
                name: "events_max_gap_ms",
                table: "tests");
        }
    }
}
