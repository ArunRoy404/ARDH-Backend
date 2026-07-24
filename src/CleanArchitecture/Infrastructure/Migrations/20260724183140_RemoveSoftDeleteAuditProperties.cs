using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSoftDeleteAuditProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "restored_by",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "users");

            migrationBuilder.DropColumn(
                name: "RestoredBy",
                table: "users");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "restored_by",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "tenant_move_out_records");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "tenant_move_out_records");

            migrationBuilder.DropColumn(
                name: "restored_by",
                table: "tenant_move_out_records");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "owners");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "owners");

            migrationBuilder.DropColumn(
                name: "restored_by",
                table: "owners");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "maintenance_requests");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "maintenance_requests");

            migrationBuilder.DropColumn(
                name: "restored_by",
                table: "maintenance_requests");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "income_records");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "income_records");

            migrationBuilder.DropColumn(
                name: "restored_by",
                table: "income_records");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "expense_records");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "expense_records");

            migrationBuilder.DropColumn(
                name: "restored_by",
                table: "expense_records");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "restored_by",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "buildings");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "buildings");

            migrationBuilder.DropColumn(
                name: "RestoredBy",
                table: "buildings");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "restored_by",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "amc_contracts");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "amc_contracts");

            migrationBuilder.DropColumn(
                name: "restored_by",
                table: "amc_contracts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "vendors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                table: "vendors",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "restored_by",
                table: "vendors",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RestoredBy",
                table: "users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                table: "tenants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "restored_by",
                table: "tenants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "tenant_move_out_records",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                table: "tenant_move_out_records",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "restored_by",
                table: "tenant_move_out_records",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "owners",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                table: "owners",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "restored_by",
                table: "owners",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "maintenance_requests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                table: "maintenance_requests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "restored_by",
                table: "maintenance_requests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "income_records",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                table: "income_records",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "restored_by",
                table: "income_records",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "expense_records",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                table: "expense_records",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "restored_by",
                table: "expense_records",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "equipment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                table: "equipment",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "restored_by",
                table: "equipment",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "buildings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "buildings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RestoredBy",
                table: "buildings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "apartments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                table: "apartments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "restored_by",
                table: "apartments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "amc_contracts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                table: "amc_contracts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "restored_by",
                table: "amc_contracts",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
