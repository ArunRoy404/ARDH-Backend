using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixDateOnlyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Normalize any pre-existing non-midnight values (e.g. maintenance_requests.last_completed_date,
            // previously set via DateTime.UtcNow instead of DateTime.UtcNow.Date, and anything propagated
            // from it into scheduled_date/start_date by the recurrence rollover job) before the column type
            // change below, so no row silently loses a time component it wasn't expected to have.
            migrationBuilder.Sql(@"
                UPDATE maintenance_requests SET start_date = CAST(start_date AS date) WHERE start_date IS NOT NULL;
                UPDATE maintenance_requests SET scheduled_date = CAST(scheduled_date AS date) WHERE scheduled_date IS NOT NULL;
                UPDATE maintenance_requests SET last_completed_date = CAST(last_completed_date AS date) WHERE last_completed_date IS NOT NULL;
                UPDATE income_records SET payment_date = CAST(payment_date AS date);
                UPDATE expense_records SET expense_date = CAST(expense_date AS date);
                UPDATE equipment SET warranty_expiry_date = CAST(warranty_expiry_date AS date) WHERE warranty_expiry_date IS NOT NULL;
                UPDATE equipment SET install_date = CAST(install_date AS date);
                UPDATE amc_contracts SET start_date = CAST(start_date AS date);
                UPDATE amc_contracts SET end_date = CAST(end_date AS date);
            ");

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_date",
                table: "maintenance_requests",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "scheduled_date",
                table: "maintenance_requests",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_completed_date",
                table: "maintenance_requests",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "payment_date",
                table: "income_records",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expense_date",
                table: "expense_records",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "warranty_expiry_date",
                table: "equipment",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "install_date",
                table: "equipment",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_date",
                table: "amc_contracts",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "end_date",
                table: "amc_contracts",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "start_date",
                table: "maintenance_requests",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "scheduled_date",
                table: "maintenance_requests",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_completed_date",
                table: "maintenance_requests",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "payment_date",
                table: "income_records",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expense_date",
                table: "expense_records",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTime>(
                name: "warranty_expiry_date",
                table: "equipment",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "install_date",
                table: "equipment",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_date",
                table: "amc_contracts",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTime>(
                name: "end_date",
                table: "amc_contracts",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");
        }
    }
}
