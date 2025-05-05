using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MijnThuis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedTariffFormulaExpressens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ConsumptionCentsPerKWh",
                table: "DAY_AHEAD_ENERGY_PRICES",
                type: "decimal(9,3)",
                precision: 9,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ConsumptionTariffFormulaExpression",
                table: "DAY_AHEAD_ENERGY_PRICES",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "InjectionCentsPerKWh",
                table: "DAY_AHEAD_ENERGY_PRICES",
                type: "decimal(9,3)",
                precision: 9,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "InjectionTariffFormulaExpression",
                table: "DAY_AHEAD_ENERGY_PRICES",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsumptionCentsPerKWh",
                table: "DAY_AHEAD_ENERGY_PRICES");

            migrationBuilder.DropColumn(
                name: "ConsumptionTariffFormulaExpression",
                table: "DAY_AHEAD_ENERGY_PRICES");

            migrationBuilder.DropColumn(
                name: "InjectionCentsPerKWh",
                table: "DAY_AHEAD_ENERGY_PRICES");

            migrationBuilder.DropColumn(
                name: "InjectionTariffFormulaExpression",
                table: "DAY_AHEAD_ENERGY_PRICES");
        }
    }
}
