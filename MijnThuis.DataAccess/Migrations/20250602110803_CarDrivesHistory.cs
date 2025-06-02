using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MijnThuis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CarDrivesHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CAR_DRIVES_HISTORY",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SysId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TessieId = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartingLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EndingLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartingOdometer = table.Column<int>(type: "int", nullable: false),
                    EndingOdometer = table.Column<int>(type: "int", nullable: false),
                    StartingBattery = table.Column<int>(type: "int", nullable: false),
                    EndingBattery = table.Column<int>(type: "int", nullable: false),
                    EnergyUsed = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    RangeUsed = table.Column<int>(type: "int", nullable: false),
                    AverageSpeed = table.Column<int>(type: "int", nullable: false),
                    MaximumSpeed = table.Column<int>(type: "int", nullable: false),
                    Distance = table.Column<int>(type: "int", nullable: false),
                    AverageInsideTemperature = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    AverageOutsideTemperature = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CAR_DRIVES_HISTORY", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CAR_DRIVES_HISTORY_StartedAt_EndedAt",
                table: "CAR_DRIVES_HISTORY",
                columns: new[] { "StartedAt", "EndedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CAR_DRIVES_HISTORY_SysId",
                table: "CAR_DRIVES_HISTORY",
                column: "SysId")
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_CAR_DRIVES_HISTORY_TessieId",
                table: "CAR_DRIVES_HISTORY",
                column: "TessieId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CAR_DRIVES_HISTORY");
        }
    }
}
