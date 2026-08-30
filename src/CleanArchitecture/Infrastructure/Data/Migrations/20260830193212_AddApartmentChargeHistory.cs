using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApartmentChargeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "apartment_charge_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    apartment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    charge_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    effective_from = table.Column<DateTime>(type: "date", nullable: false),
                    effective_to = table.Column<DateTime>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_apartment_charge_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_apartment_charge_history_apartments_apartment_id",
                        column: x => x.apartment_id,
                        principalTable: "apartments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_apartment_charge_history_apartment_id_charge_type_effective_from",
                table: "apartment_charge_history",
                columns: new[] { "apartment_id", "charge_type", "effective_from" });

            // Backfill: seed one open-ended charge-history segment per existing apartment for each
            // of the three tracked charge types, so pre-existing apartments aren't invisible to the
            // new charge-history endpoint.
            migrationBuilder.Sql(@"
                INSERT INTO apartment_charge_history (id, apartment_id, charge_type, amount, effective_from, effective_to, created_at)
                SELECT NEWID(), id, 'Rent', expected_rent, CAST(created_at AS date), NULL, GETUTCDATE()
                FROM apartments
                WHERE IsDeleted = 0;

                INSERT INTO apartment_charge_history (id, apartment_id, charge_type, amount, effective_from, effective_to, created_at)
                SELECT NEWID(), id, 'Maintenance', maintenance_charge, CAST(created_at AS date), NULL, GETUTCDATE()
                FROM apartments
                WHERE IsDeleted = 0;

                INSERT INTO apartment_charge_history (id, apartment_id, charge_type, amount, effective_from, effective_to, created_at)
                SELECT NEWID(), id, 'Water', water_charge, CAST(created_at AS date), NULL, GETUTCDATE()
                FROM apartments
                WHERE IsDeleted = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "apartment_charge_history");
        }
    }
}
