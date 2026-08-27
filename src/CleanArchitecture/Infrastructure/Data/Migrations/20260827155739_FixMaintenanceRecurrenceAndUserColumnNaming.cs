using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixMaintenanceRecurrenceAndUserColumnNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReceiveEmailNotifications",
                table: "users",
                newName: "receive_email_notifications");

            migrationBuilder.AddColumn<bool>(
                name: "next_cycle_generated",
                table: "maintenance_requests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "next_cycle_generated",
                table: "maintenance_requests");

            migrationBuilder.RenameColumn(
                name: "receive_email_notifications",
                table: "users",
                newName: "ReceiveEmailNotifications");
        }
    }
}
