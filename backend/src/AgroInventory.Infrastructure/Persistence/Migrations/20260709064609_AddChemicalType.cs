using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChemicalType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "chemical_details",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "type",
                table: "chemical_details");
        }
    }
}
