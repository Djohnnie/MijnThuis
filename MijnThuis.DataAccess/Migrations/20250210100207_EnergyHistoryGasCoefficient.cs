using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MijnThuis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class EnergyHistoryGasCoefficient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GasCoefficient",
                table: "ENERGY_HISTORY",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalGasKwh",
                table: "ENERGY_HISTORY",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalGasKwhDelta",
                table: "ENERGY_HISTORY",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GasCoefficient",
                table: "ENERGY_HISTORY");

            migrationBuilder.DropColumn(
                name: "TotalGasKwh",
                table: "ENERGY_HISTORY");

            migrationBuilder.DropColumn(
                name: "TotalGasKwhDelta",
                table: "ENERGY_HISTORY");
        }
    }
}
