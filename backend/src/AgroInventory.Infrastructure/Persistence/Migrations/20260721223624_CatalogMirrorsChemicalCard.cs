using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogMirrorsChemicalCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "active_ingredient",
                table: "canonical_chemicals");

            migrationBuilder.DropColumn(
                name: "concentration",
                table: "canonical_chemicals");

            migrationBuilder.DropColumn(
                name: "formulation",
                table: "canonical_chemicals");

            migrationBuilder.DropColumn(
                name: "registration_number",
                table: "canonical_chemicals");

            migrationBuilder.AddColumn<string>(
                name: "comment",
                table: "canonical_chemicals",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "measure_unit",
                table: "canonical_chemicals",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "canonical_chemicals",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "canonical_chemical_crops",
                columns: table => new
                {
                    canonical_chemical_id = table.Column<Guid>(type: "uuid", nullable: false),
                    crop_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_canonical_chemical_crops", x => new { x.canonical_chemical_id, x.crop_id });
                    table.ForeignKey(
                        name: "fk_canonical_chemical_crops_canonical_chemicals_canonical_chem",
                        column: x => x.canonical_chemical_id,
                        principalTable: "canonical_chemicals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_canonical_chemical_crops_crops_crop_id",
                        column: x => x.crop_id,
                        principalTable: "crops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_canonical_chemical_crops_crop_id",
                table: "canonical_chemical_crops",
                column: "crop_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "canonical_chemical_crops");

            migrationBuilder.DropColumn(
                name: "comment",
                table: "canonical_chemicals");

            migrationBuilder.DropColumn(
                name: "measure_unit",
                table: "canonical_chemicals");

            migrationBuilder.DropColumn(
                name: "type",
                table: "canonical_chemicals");

            migrationBuilder.AddColumn<string>(
                name: "active_ingredient",
                table: "canonical_chemicals",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "concentration",
                table: "canonical_chemicals",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "formulation",
                table: "canonical_chemicals",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registration_number",
                table: "canonical_chemicals",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
