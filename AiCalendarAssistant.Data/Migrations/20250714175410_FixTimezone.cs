using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCalendarAssistant.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixTimezone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "88bd4ce9-aece-4378-b9d5-1e5cff74b80c",
                columns: new[] { "PasswordHash", "SecurityStamp", "TimeZone" },
                values: new object[] { "AQAAAAIAAYagAAAAEB5WBc7U/aiUcJDUpz4ktXOtVoY/b9rXI6rWtXwP6l81rRQAnXUeNgEipPhnNtH2JQ==", "98875225-7592-4965-b455-c2d06c69ed98", "GMT Standard Time" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "88bd4ce9-aece-4378-b9d5-1e5cff74b80c",
                columns: new[] { "PasswordHash", "SecurityStamp", "TimeZone" },
                values: new object[] { "AQAAAAIAAYagAAAAEBbwExT2bV4Rh3XiSlbbxLU8WBfbM309jP0j7Gmtpc8WerAZHRNVYuJGcL8h1VUDIg==", "e6636458-cd31-4402-a885-794e6756d8e4", "UTC" });
        }
    }
}
