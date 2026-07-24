using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditPropertiesToAllModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "vendors",
                type: "uniqueidentifier",
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

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "vendors",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "users",
                type: "uniqueidentifier",
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

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "tenants",
                type: "uniqueidentifier",
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

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "tenants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "tenant_move_out_records",
                type: "uniqueidentifier",
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

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "tenant_move_out_records",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "owners",
                type: "uniqueidentifier",
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

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "owners",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "equipment",
                type: "uniqueidentifier",
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

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "equipment",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "buildings",
                type: "uniqueidentifier",
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

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "buildings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "apartments",
                type: "uniqueidentifier",
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

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "apartments",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "restored_by",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "users");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "users");

            migrationBuilder.DropColumn(
                name: "RestoredBy",
                table: "users");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "users");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "restored_by",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "tenant_move_out_records");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "tenant_move_out_records");

            migrationBuilder.DropColumn(
                name: "restored_by",
                table: "tenant_move_out_records");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "tenant_move_out_records");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "owners");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "owners");

            migrationBuilder.DropColumn(
                name: "restored_by",
                table: "owners");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "owners");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "restored_by",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "buildings");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "buildings");

            migrationBuilder.DropColumn(
                name: "RestoredBy",
                table: "buildings");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "buildings");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "restored_by",
                table: "apartments");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "apartments");
        }
    }
}
