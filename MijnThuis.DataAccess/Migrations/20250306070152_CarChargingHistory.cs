using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MijnThuis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CarChargingHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CAR_CHARGING_HISTORY",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SysId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChargingSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChargingAmps = table.Column<int>(type: "int", nullable: false),
                    ChargingDuration = table.Column<TimeSpan>(type: "time", nullable: false),
                    EnergyCharged = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CAR_CHARGING_HISTORY", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CAR_CHARGING_HISTORY_ChargingSessionId",
                table: "CAR_CHARGING_HISTORY",
                column: "ChargingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CAR_CHARGING_HISTORY_SysId",
                table: "CAR_CHARGING_HISTORY",
                column: "SysId")
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_CAR_CHARGING_HISTORY_Timestamp",
                table: "CAR_CHARGING_HISTORY",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CAR_CHARGING_HISTORY");
        }
    }
}