using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MijnThuis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class BatteryEnergyHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BATTERY_ENERGY_HISTORY",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SysId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RatedEnergy = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AvailableEnergy = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StateOfCharge = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CalculatedStateOfHealth = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StateOfHealth = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BATTERY_ENERGY_HISTORY", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SOLAR_POWER_HISTORY_Date",
                table: "SOLAR_POWER_HISTORY",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_SOLAR_ENERGY_HISTORY_Date",
                table: "SOLAR_ENERGY_HISTORY",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_BATTERY_ENERGY_HISTORY_Date",
                table: "BATTERY_ENERGY_HISTORY",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_BATTERY_ENERGY_HISTORY_SysId",
                table: "BATTERY_ENERGY_HISTORY",
                column: "SysId")
                .Annotation("SqlServer:Clustered", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BATTERY_ENERGY_HISTORY");

            migrationBuilder.DropIndex(
                name: "IX_SOLAR_POWER_HISTORY_Date",
                table: "SOLAR_POWER_HISTORY");

            migrationBuilder.DropIndex(
                name: "IX_SOLAR_ENERGY_HISTORY_Date",
                table: "SOLAR_ENERGY_HISTORY");
        }
    }
}
