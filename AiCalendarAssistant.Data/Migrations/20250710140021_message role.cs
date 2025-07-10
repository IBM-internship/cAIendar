using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCalendarAssistant.Data.Migrations
{
    /// <inheritdoc />
    public partial class messagerole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Messages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Importance",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Importance",
                table: "Events");
        }
    }
}
