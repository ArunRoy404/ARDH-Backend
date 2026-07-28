using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantsAndMoveOutTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    building_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    apartment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    id_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    id_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    id_proof_attachment_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    move_in_date = table.Column<DateTime>(type: "date", nullable: false),
                    lease_start_date = table.Column<DateTime>(type: "date", nullable: false),
                    lease_end_date = table.Column<DateTime>(type: "date", nullable: true),
                    monthly_rent = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    security_deposit = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    emergency_contact_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    emergency_contact_phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                    table.ForeignKey(
                        name: "FK_tenants_apartments_apartment_id",
                        column: x => x.apartment_id,
                        principalTable: "apartments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenants_buildings_building_id",
                        column: x => x.building_id,
                        principalTable: "buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_move_out_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    apartment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    move_out_date = table.Column<DateTime>(type: "date", nullable: false),
                    pending_rent = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    damage_amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    refund_amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    id_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    handover_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    processed_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_move_out_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_tenant_move_out_records_apartments_apartment_id",
                        column: x => x.apartment_id,
                        principalTable: "apartments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_move_out_records_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_move_out_records_users_processed_by",
                        column: x => x.processed_by,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_move_out_records_apartment_id",
                table: "tenant_move_out_records",
                column: "apartment_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_move_out_records_processed_by",
                table: "tenant_move_out_records",
                column: "processed_by");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_move_out_records_tenant_id",
                table: "tenant_move_out_records",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_apartment_id",
                table: "tenants",
                column: "apartment_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_building_id",
                table: "tenants",
                column: "building_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_move_out_records");

            migrationBuilder.DropTable(
                name: "tenants");
        }
    }
}
