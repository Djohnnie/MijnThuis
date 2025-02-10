using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MijnThuis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class EnergyHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ENERGY_HISTORY",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SysId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActiveTarrif = table.Column<byte>(type: "tinyint", nullable: false),
                    TotalImport = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalImportDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tarrif1Import = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tarrif1ImportDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tarrif2Import = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tarrif2ImportDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalExport = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalExportDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tarrif1Export = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tarrif1ExportDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tarrif2Export = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tarrif2ExportDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalGas = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalGasDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MonthlyPowerPeak = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ENERGY_HISTORY", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ENERGY_HISTORY_Date",
                table: "ENERGY_HISTORY",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ENERGY_HISTORY_SysId",
                table: "ENERGY_HISTORY",
                column: "SysId")
                .Annotation("SqlServer:Clustered", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ENERGY_HISTORY");
        }
    }
}
