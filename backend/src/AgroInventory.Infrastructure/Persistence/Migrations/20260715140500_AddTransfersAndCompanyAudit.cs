using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransfersAndCompanyAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "company_id",
                table: "audit_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "target_warehouse_id",
                table: "inventory_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_company_id",
                table: "audit_logs",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_target_warehouse_id",
                table: "inventory_movements",
                column: "target_warehouse_id");

            migrationBuilder.AddForeignKey(
                name: "fk_audit_logs_companies_company_id",
                table: "audit_logs",
                column: "company_id",
                principalTable: "companies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_movements_warehouses_target_warehouse_id",
                table: "inventory_movements",
                column: "target_warehouse_id",
                principalTable: "warehouses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_audit_logs_companies_company_id",
                table: "audit_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_movements_warehouses_target_warehouse_id",
                table: "inventory_movements");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_company_id",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_inventory_movements_target_warehouse_id",
                table: "inventory_movements");

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "target_warehouse_id",
                table: "inventory_movements");
        }
    }
}
