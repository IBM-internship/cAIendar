using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCalendarAssistant.Data.Migrations
{
    /// <inheritdoc />
    public partial class taskdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "Tasks",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "Tasks");
        }
    }
}
