using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MijnThuis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SolarForecastHistory_Fixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ForecastedEnergyTomorrowPlusOne",
                table: "SOLAR_FORECAST_HISTORY");

            migrationBuilder.RenameColumn(
                name: "ForecastedEnergyTomorrowPlusTwo",
                table: "SOLAR_FORECAST_HISTORY",
                newName: "ForecastedEnergyDayAfterTomorrow");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ForecastedEnergyDayAfterTomorrow",
                table: "SOLAR_FORECAST_HISTORY",
                newName: "ForecastedEnergyTomorrowPlusTwo");

            migrationBuilder.AddColumn<decimal>(
                name: "ForecastedEnergyTomorrowPlusOne",
                table: "SOLAR_FORECAST_HISTORY",
                type: "decimal(9,3)",
                precision: 9,
                scale: 3,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
