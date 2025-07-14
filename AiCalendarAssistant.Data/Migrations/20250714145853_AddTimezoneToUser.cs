using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCalendarAssistant.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTimezoneToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserDiscription",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "88bd4ce9-aece-4378-b9d5-1e5cff74b80c",
                columns: new[] { "PasswordHash", "SecurityStamp", "TimeZone" },
                values: new object[] { "AQAAAAIAAYagAAAAEBbwExT2bV4Rh3XiSlbbxLU8WBfbM309jP0j7Gmtpc8WerAZHRNVYuJGcL8h1VUDIg==", "e6636458-cd31-4402-a885-794e6756d8e4", "UTC" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "UserDiscription",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "88bd4ce9-aece-4378-b9d5-1e5cff74b80c",
                columns: new[] { "PasswordHash", "SecurityStamp" },
                values: new object[] { "AQAAAAIAAYagAAAAEMmdEsC1XzVQ72PaPaJYhr7SVKeE047UkOeylkPzccadqHnLb8oowRGCK4Gvtp/vWQ==", "bd47d867-7a0d-4f10-bd58-294e34bbcb1d" });
        }
    }
}
