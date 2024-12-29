using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MijnThuis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SolarHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SOLAR_HISTORY",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SysId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Import = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Export = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Production = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductionToHome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductionToBattery = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductionToGrid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Consumption = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ConsumptionFromBattery = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ConsumptionFromSolar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ConsumptionFromGrid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataCollected = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SOLAR_HISTORY", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SOLAR_HISTORY_SysId",
                table: "SOLAR_HISTORY",
                column: "SysId")
                .Annotation("SqlServer:Clustered", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SOLAR_HISTORY");
        }
    }
}
