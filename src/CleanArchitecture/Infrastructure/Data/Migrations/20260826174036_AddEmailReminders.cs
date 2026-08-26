using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReceiveEmailNotifications",
                table: "users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_completed_date",
                table: "maintenance_requests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "email_reminder_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reminder_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sent_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_reminder_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_reminder_logs_reminder_type_entity_id_user_id",
                table: "email_reminder_logs",
                columns: new[] { "reminder_type", "entity_id", "user_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_reminder_logs");

            migrationBuilder.DropColumn(
                name: "ReceiveEmailNotifications",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_completed_date",
                table: "maintenance_requests");
        }
    }
}
