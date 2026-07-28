using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAmcContractsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "amc_contracts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    amc_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    contract_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    contract_title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    contract_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    equipment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    contract_amount = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    payment_terms = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    service_frequency = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    coverage_details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    exclusions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    document_link = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    remarks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    restored_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amc_contracts", x => x.id);
                    table.ForeignKey(
                        name: "FK_amc_contracts_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_amc_contracts_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_amc_contracts_amc_code",
                table: "amc_contracts",
                column: "amc_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_amc_contracts_equipment_id",
                table: "amc_contracts",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_amc_contracts_vendor_id",
                table: "amc_contracts",
                column: "vendor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "amc_contracts");
        }
    }
}
