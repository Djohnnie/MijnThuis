using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MijnThuis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DayAheadEnergyPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DAY_AHEAD_ENERGY_PRICES",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SysId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    From = table.Column<DateTime>(type: "datetime2", nullable: false),
                    To = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EuroPerMWh = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DAY_AHEAD_ENERGY_PRICES", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DAY_AHEAD_ENERGY_PRICES_From",
                table: "DAY_AHEAD_ENERGY_PRICES",
                column: "From");

            migrationBuilder.CreateIndex(
                name: "IX_DAY_AHEAD_ENERGY_PRICES_SysId",
                table: "DAY_AHEAD_ENERGY_PRICES",
                column: "SysId")
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_DAY_AHEAD_ENERGY_PRICES_To",
                table: "DAY_AHEAD_ENERGY_PRICES",
                column: "To");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DAY_AHEAD_ENERGY_PRICES");
        }
    }
}
