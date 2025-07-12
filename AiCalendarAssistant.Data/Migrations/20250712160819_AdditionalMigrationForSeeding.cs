using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCalendarAssistant.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdditionalMigrationForSeeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "IsDeleted", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserDiscription", "UserName" },
                values: new object[] { "88bd4ce9-aece-4378-b9d5-1e5cff74b80c", 0, "d1f3b2a4-5c8e-4b7e-9c0f-6a2d3f4e5b6a", "user1@email.com", false, false, false, null, "USER1@EMAIL.COM", "USER1@EMAIL.COM", "AQAAAAIAAYagAAAAEMmdEsC1XzVQ72PaPaJYhr7SVKeE047UkOeylkPzccadqHnLb8oowRGCK4Gvtp/vWQ==", null, false, "bd47d867-7a0d-4f10-bd58-294e34bbcb1d", false, "", "user1@email.com" });

            migrationBuilder.InsertData(
                table: "Emails",
                columns: new[] { "Id", "Body", "CreatedOn", "GmailMessageId", "IsDeleted", "IsProcessed", "MessageId", "RecievingUserId", "SendingUserEmail", "ThreadId", "Title" },
                values: new object[] { -1, "This is the body of the first test email.", new DateTime(2025, 1, 1, 9, 0, 0, 0, DateTimeKind.Unspecified), "1234567890abcdef", false, false, "message1234567890", "88bd4ce9-aece-4378-b9d5-1e5cff74b80c", "sendinguser@email.com", "thread1234567890", "Test Email 1" });

            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "Id", "Color", "Description", "End", "EventCreatedFromEmailId", "Importance", "IsAllDay", "IsDeleted", "IsInPerson", "Location", "MeetingLink", "Start", "Title", "UserId" },
                values: new object[] { -1, "#FF5733", "This is the first event.", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), -1, 1, false, false, true, "Conference Room A", "", new DateTime(2025, 1, 1, 10, 0, 0, 0, DateTimeKind.Unspecified), "Event 1", "88bd4ce9-aece-4378-b9d5-1e5cff74b80c" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: -1);

            migrationBuilder.DeleteData(
                table: "Emails",
                keyColumn: "Id",
                keyValue: -1);

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "88bd4ce9-aece-4378-b9d5-1e5cff74b80c");
        }
    }
}
