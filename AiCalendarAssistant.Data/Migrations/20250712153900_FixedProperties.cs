using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCalendarAssistant.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixedProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserDiscription",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserDiscription",
                table: "AspNetUsers");
        }
    }
}
