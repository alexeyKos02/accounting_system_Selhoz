using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldAreaAndCrop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "area_hectares",
                table: "fields",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "current_crop_id",
                table: "fields",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_fields_current_crop_id",
                table: "fields",
                column: "current_crop_id");

            migrationBuilder.AddForeignKey(
                name: "fk_fields_crops_current_crop_id",
                table: "fields",
                column: "current_crop_id",
                principalTable: "crops",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_fields_crops_current_crop_id",
                table: "fields");

            migrationBuilder.DropIndex(
                name: "ix_fields_current_crop_id",
                table: "fields");

            migrationBuilder.DropColumn(
                name: "area_hectares",
                table: "fields");

            migrationBuilder.DropColumn(
                name: "current_crop_id",
                table: "fields");
        }
    }
}
