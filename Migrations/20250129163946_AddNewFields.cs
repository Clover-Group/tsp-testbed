using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TspTestbed.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "datetime_field",
                table: "tests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "query",
                table: "tests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "started",
                table: "test_runs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "table_name",
                table: "sinks",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "datetime_field",
                table: "tests");

            migrationBuilder.DropColumn(
                name: "query",
                table: "tests");

            migrationBuilder.DropColumn(
                name: "started",
                table: "test_runs");

            migrationBuilder.DropColumn(
                name: "table_name",
                table: "sinks");
        }
    }
}
