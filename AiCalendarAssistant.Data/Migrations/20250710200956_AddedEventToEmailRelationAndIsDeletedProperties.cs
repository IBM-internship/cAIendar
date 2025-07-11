using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCalendarAssistant.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedEventToEmailRelationAndIsDeletedProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserNotes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EventCreatedFromEmailId",
                table: "Events",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Emails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Chats",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventCreatedFromEmailId",
                table: "Events",
                column: "EventCreatedFromEmailId",
                unique: true,
                filter: "[EventCreatedFromEmailId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Emails_EventCreatedFromEmailId",
                table: "Events",
                column: "EventCreatedFromEmailId",
                principalTable: "Emails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Emails_EventCreatedFromEmailId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_EventCreatedFromEmailId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserNotes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "EventCreatedFromEmailId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AspNetUsers");
        }
    }
}
