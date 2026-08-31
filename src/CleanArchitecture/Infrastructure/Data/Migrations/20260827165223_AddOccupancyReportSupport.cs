using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOccupancyReportSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tenants_apartment_id",
                table: "tenants");

            migrationBuilder.CreateTable(
                name: "tenant_rent_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    monthly_rent = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    effective_from = table.Column<DateTime>(type: "date", nullable: false),
                    effective_to = table.Column<DateTime>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_rent_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_tenant_rent_history_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenants_apartment_id_lease_start_date_lease_end_date",
                table: "tenants",
                columns: new[] { "apartment_id", "lease_start_date", "lease_end_date" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_rent_history_tenant_id_effective_from",
                table: "tenant_rent_history",
                columns: new[] { "tenant_id", "effective_from" });

            // Backfill: seed one open-ended rent-history segment per existing tenant so the
            // occupancy report can price leases that predate this feature.
            migrationBuilder.Sql(@"
                INSERT INTO tenant_rent_history (id, tenant_id, monthly_rent, effective_from, effective_to, created_at)
                SELECT NEWID(), id, monthly_rent, lease_start_date, NULL, GETUTCDATE()
                FROM tenants
                WHERE IsDeleted = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_rent_history");

            migrationBuilder.DropIndex(
                name: "IX_tenants_apartment_id_lease_start_date_lease_end_date",
                table: "tenants");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_apartment_id",
                table: "tenants",
                column: "apartment_id");
        }
    }
}
