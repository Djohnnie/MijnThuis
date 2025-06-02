using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MijnThuis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CarChargesHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CAR_CHARGES_HISTORY",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SysId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TessieId = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationFriendlyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSupercharger = table.Column<bool>(type: "bit", nullable: false),
                    IsFastCharger = table.Column<bool>(type: "bit", nullable: false),
                    Odometer = table.Column<int>(type: "int", nullable: false),
                    EnergyAdded = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    EnergyUsed = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    RangeAdded = table.Column<int>(type: "int", nullable: false),
                    BatteryLevelStart = table.Column<int>(type: "int", nullable: false),
                    BatteryLevelEnd = table.Column<int>(type: "int", nullable: false),
                    DistanceSinceLastCharge = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CAR_CHARGES_HISTORY", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CAR_CHARGES_HISTORY_StartedAt_EndedAt",
                table: "CAR_CHARGES_HISTORY",
                columns: new[] { "StartedAt", "EndedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CAR_CHARGES_HISTORY_SysId",
                table: "CAR_CHARGES_HISTORY",
                column: "SysId")
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_CAR_CHARGES_HISTORY_TessieId",
                table: "CAR_CHARGES_HISTORY",
                column: "TessieId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CAR_CHARGES_HISTORY");
        }
    }
}
