using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    building_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    brand = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    model = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    serial_number = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    install_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    warranty_expiry_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    amc_vendor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    amc_expiry_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_service_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    next_service_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    attachment_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment", x => x.id);
                    table.ForeignKey(
                        name: "FK_equipment_buildings_building_id",
                        column: x => x.building_id,
                        principalTable: "buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_vendors_amc_vendor_id",
                        column: x => x.amc_vendor_id,
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_amc_vendor_id",
                table: "equipment",
                column: "amc_vendor_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_building_id",
                table: "equipment",
                column: "building_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "equipment");
        }
    }
}
