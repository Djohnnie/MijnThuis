using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MijnThuis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SolarForecastPeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SOLAR_FORECAST_PERIODS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SysId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFetched = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ForecastedEnergy = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    ActualEnergy = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SOLAR_FORECAST_PERIODS", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SOLAR_FORECAST_PERIODS_DataFetched",
                table: "SOLAR_FORECAST_PERIODS",
                column: "DataFetched");

            migrationBuilder.CreateIndex(
                name: "IX_SOLAR_FORECAST_PERIODS_SysId",
                table: "SOLAR_FORECAST_PERIODS",
                column: "SysId")
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_SOLAR_FORECAST_PERIODS_Timestamp",
                table: "SOLAR_FORECAST_PERIODS",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SOLAR_FORECAST_PERIODS");
        }
    }
}
