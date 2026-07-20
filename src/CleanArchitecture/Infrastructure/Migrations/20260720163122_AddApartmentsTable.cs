using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApartmentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "apartments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    building_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    owner_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    nestaway_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    flat_number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    floor = table.Column<int>(type: "int", nullable: false),
                    apartment_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    area_sqft = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    bedrooms = table.Column<int>(type: "int", nullable: false),
                    bathrooms = table.Column<int>(type: "int", nullable: false),
                    has_balcony = table.Column<bool>(type: "bit", nullable: false),
                    parking_slot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    expected_rent = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    maintenance_charge = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    water_charge = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    occupancy_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    current_tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_apartments", x => x.id);
                    table.ForeignKey(
                        name: "FK_apartments_buildings_building_id",
                        column: x => x.building_id,
                        principalTable: "buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_apartments_owners_owner_id",
                        column: x => x.owner_id,
                        principalTable: "owners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_apartments_building_id",
                table: "apartments",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_apartments_owner_id",
                table: "apartments",
                column: "owner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "apartments");
        }
    }
}
