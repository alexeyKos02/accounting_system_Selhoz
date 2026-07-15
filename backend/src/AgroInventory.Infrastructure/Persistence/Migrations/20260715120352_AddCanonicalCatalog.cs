using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCanonicalCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "canonical_chemical_id",
                table: "inventory_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "canonical_chemicals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    canonical_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    manufacturer = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    active_ingredient = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    concentration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    formulation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    registration_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_canonical_chemicals", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_canonical_chemical_id",
                table: "inventory_items",
                column: "canonical_chemical_id");

            migrationBuilder.CreateIndex(
                name: "ix_canonical_chemicals_canonical_name",
                table: "canonical_chemicals",
                column: "canonical_name");

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_items_canonical_chemicals_canonical_chemical_id",
                table: "inventory_items",
                column: "canonical_chemical_id",
                principalTable: "canonical_chemicals",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_items_canonical_chemicals_canonical_chemical_id",
                table: "inventory_items");

            migrationBuilder.DropTable(
                name: "canonical_chemicals");

            migrationBuilder.DropIndex(
                name: "ix_inventory_items_canonical_chemical_id",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "canonical_chemical_id",
                table: "inventory_items");
        }
    }
}
