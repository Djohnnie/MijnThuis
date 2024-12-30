using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MijnThuis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameSolarHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SOLAR_HISTORY",
                table: "SOLAR_HISTORY");

            migrationBuilder.RenameTable(
                name: "SOLAR_HISTORY",
                newName: "SOLAR_ENERGY_HISTORY");

            migrationBuilder.RenameIndex(
                name: "IX_SOLAR_HISTORY_SysId",
                table: "SOLAR_ENERGY_HISTORY",
                newName: "IX_SOLAR_ENERGY_HISTORY_SysId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SOLAR_ENERGY_HISTORY",
                table: "SOLAR_ENERGY_HISTORY",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SOLAR_ENERGY_HISTORY",
                table: "SOLAR_ENERGY_HISTORY");

            migrationBuilder.RenameTable(
                name: "SOLAR_ENERGY_HISTORY",
                newName: "SOLAR_HISTORY");

            migrationBuilder.RenameIndex(
                name: "IX_SOLAR_ENERGY_HISTORY_SysId",
                table: "SOLAR_HISTORY",
                newName: "IX_SOLAR_HISTORY_SysId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SOLAR_HISTORY",
                table: "SOLAR_HISTORY",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);
        }
    }
}
