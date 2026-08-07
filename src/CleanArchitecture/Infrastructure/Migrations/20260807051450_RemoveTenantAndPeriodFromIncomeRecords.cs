using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTenantAndPeriodFromIncomeRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_income_records_tenants_tenant_id",
                table: "income_records");

            migrationBuilder.DropIndex(
                name: "IX_income_records_tenant_id",
                table: "income_records");

            migrationBuilder.DropColumn(
                name: "period",
                table: "income_records");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "income_records");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "period",
                table: "income_records",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "income_records",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_income_records_tenant_id",
                table: "income_records",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "FK_income_records_tenants_tenant_id",
                table: "income_records",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
