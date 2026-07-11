using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "field_id",
                table: "inventory_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "fields",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fields", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_field_id",
                table: "inventory_movements",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "ix_fields_number",
                table: "fields",
                column: "number",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_movements_fields_field_id",
                table: "inventory_movements",
                column: "field_id",
                principalTable: "fields",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_movements_fields_field_id",
                table: "inventory_movements");

            migrationBuilder.DropTable(
                name: "fields");

            migrationBuilder.DropIndex(
                name: "ix_inventory_movements_field_id",
                table: "inventory_movements");

            migrationBuilder.DropColumn(
                name: "field_id",
                table: "inventory_movements");
        }
    }
}
