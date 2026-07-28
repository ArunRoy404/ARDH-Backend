using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "expense_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    expense_head = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    specific_item = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    vendor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    nature = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    entity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    building_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    apartment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    expense_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    payment_method = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    reference = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    attachment_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tanker_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    time_of_delivery = table.Column<DateTime>(type: "datetime2", nullable: true),
                    delivery_driver_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    manager_in_attendance = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    liters_filled = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_expense_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_expense_records_apartments_apartment_id",
                        column: x => x.apartment_id,
                        principalTable: "apartments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_expense_records_buildings_building_id",
                        column: x => x.building_id,
                        principalTable: "buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_expense_records_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_expense_records_apartment_id",
                table: "expense_records",
                column: "apartment_id");

            migrationBuilder.CreateIndex(
                name: "IX_expense_records_building_id",
                table: "expense_records",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_expense_records_vendor_id",
                table: "expense_records",
                column: "vendor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expense_records");
        }
    }
}
