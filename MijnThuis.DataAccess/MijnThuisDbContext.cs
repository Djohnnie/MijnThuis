using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MijnThuis.DataAccess.Entities;

namespace MijnThuis.DataAccess;

public class MijnThuisDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public DbSet<Flag> Flags { get; set; }
    public DbSet<SolarEnergyHistoryEntry> SolarEnergyHistory { get; set; }
    public DbSet<SolarPowerHistoryEntry> SolarPowerHistory { get; set; }
    public DbSet<BatteryEnergyHistoryEntry> BatteryEnergyHistory { get; set; }
    public DbSet<EnergyHistoryEntry> EnergyHistory { get; set; }
    public DbSet<CarChargingEnergyHistoryEntry> CarChargingHistory { get; set; }
    public DbSet<SolarForecastHistoryEntry> SolarForecastHistory { get; set; }
    public DbSet<DayAheadEnergyPricesEntry> DayAheadEnergyPrices { get; set; }
    public DbSet<EnergyInvoiceEntry> EnergyInvoices { get; set; }
    public DbSet<CarChargesHistoryEntry> CarChargesHistory { get; set; }
    public DbSet<CarDrivesHistoryEntry> CarDrivesHistory { get; set; }

    public MijnThuisDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(_configuration.GetValue<string>("CONNECTION_STRING"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Flag>(entityBuilder =>
        {
            entityBuilder.ToTable("FLAGS");
            entityBuilder.HasKey(x => x.Id).IsClustered(false);
            entityBuilder.Property(x => x.SysId).ValueGeneratedOnAdd();
            entityBuilder.HasIndex(x => x.SysId).IsClustered();
            entityBuilder.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<SolarEnergyHistoryEntry>(entityBuilder =>
        {
            entityBuilder.ToTable("SOLAR_ENERGY_HISTORY");
            entityBuilder.HasKey(x => x.Id).IsClustered(false);
            entityBuilder.Property(x => x.SysId).ValueGeneratedOnAdd();
            entityBuilder.HasIndex(x => x.SysId).IsClustered();
            entityBuilder.HasIndex(x => x.Date);
            entityBuilder.Property(x => x.Import).HasPrecision(9, 3);
            entityBuilder.Property(x => x.Export).HasPrecision(9, 3);
            entityBuilder.Property(x => x.Production).HasPrecision(9, 3);
            entityBuilder.Property(x => x.ProductionToHome).HasPrecision(9, 3);
            entityBuilder.Property(x => x.ProductionToBattery).HasPrecision(9, 3);
            entityBuilder.Property(x => x.ProductionToGrid).HasPrecision(9, 3);
            entityBuilder.Property(x => x.Consumption).HasPrecision(9, 3);
            entityBuilder.Property(x => x.ConsumptionFromBattery).HasPrecision(9, 3);
            entityBuilder.Property(x => x.ConsumptionFromSolar).HasPrecision(9, 3);
            entityBuilder.Property(x => x.ConsumptionFromGrid).HasPrecision(9, 3);
            entityBuilder.Property(x => x.ImportToBattery).HasPrecision(9, 3);
        });

        modelBuilder.Entity<SolarPowerHistoryEntry>(entityBuilder =>
        {
            entityBuilder.ToTable("SOLAR_POWER_HISTORY");
            entityBuilder.HasKey(x => x.Id).IsClustered(false);
            entityBuilder.Property(x => x.SysId).ValueGeneratedOnAdd();
            entityBuilder.HasIndex(x => x.SysId).IsClustered();
            entityBuilder.HasIndex(x => x.Date);
            entityBuilder.Property(x => x.ImportToBattery).HasPrecision(9, 3);
        });

        modelBuilder.Entity<BatteryEnergyHistoryEntry>(entityBuilder =>
        {
            entityBuilder.ToTable("BATTERY_ENERGY_HISTORY");
            entityBuilder.HasKey(x => x.Id).IsClustered(false);
            entityBuilder.Property(x => x.SysId).ValueGeneratedOnAdd();
            entityBuilder.HasIndex(x => x.SysId).IsClustered();
            entityBuilder.HasIndex(x => x.Date);
        });

        modelBuilder.Entity<EnergyHistoryEntry>(entityBuilder =>
        {
            entityBuilder.ToTable("ENERGY_HISTORY");
            entityBuilder.HasKey(x => x.Id).IsClustered(false);
            entityBuilder.Property(x => x.SysId).ValueGeneratedOnAdd();
            entityBuilder.HasIndex(x => x.SysId).IsClustered();
            entityBuilder.HasIndex(x => x.Date);
            entityBuilder.Property(x => x.TotalImport).HasPrecision(9, 3);
            entityBuilder.Property(x => x.TotalImportDelta).HasPrecision(9, 3);
            entityBuilder.Property(x => x.Tarrif1Import).HasPrecision(9, 3);
            entityBuilder.Property(x => x.Tarrif1ImportDelta).HasPrecision(9, 3);
            entityBuilder.Property(x => x.Tarrif2Import).HasPrecision(9, 3);
            entityBuilder.Property(x => x.Tarrif2ImportDelta).HasPrecision(9, 3);
            entityBuilder.Property(x => x.TotalExport).HasPrecision(9, 3);
            entityBuilder.Property(x => x.TotalExportDelta).HasPrecision(9, 3);
            entityBuilder.Property(x => x.Tarrif1Export).HasPrecision(9, 3);
            entityBuilder.Property(x => x.Tarrif1ExportDelta).HasPrecision(9, 3);
            entityBuilder.Property(x => x.Tarrif2Export).HasPrecision(9, 3);
            entityBuilder.Property(x => x.Tarrif2ExportDelta).HasPrecision(9, 3);
            entityBuilder.Property(x => x.TotalGas).HasPrecision(9, 3);
            entityBuilder.Property(x => x.TotalGasDelta).HasPrecision(9, 3);
            entityBuilder.Property(x => x.GasCoefficient).HasPrecision(9, 3);
            entityBuilder.Property(x => x.TotalGasKwh).HasPrecision(9, 3);
            entityBuilder.Property(x => x.TotalGasKwhDelta).HasPrecision(9, 3);
            entityBuilder.Property(x => x.MonthlyPowerPeak).HasPrecision(9, 3);
            entityBuilder.Property(x => x.CalculatedImportCost).HasPrecision(9, 3);
            entityBuilder.Property(x => x.CalculatedExportCost).HasPrecision(9, 3);
        });

        modelBuilder.Entity<CarChargingEnergyHistoryEntry>(entityBuilder =>
        {
            entityBuilder.ToTable("CAR_CHARGING_HISTORY");
            entityBuilder.HasKey(x => x.Id).IsClustered(false);
            entityBuilder.Property(x => x.SysId).ValueGeneratedOnAdd();
            entityBuilder.HasIndex(x => x.SysId).IsClustered();
            entityBuilder.HasIndex(x => x.Timestamp);
            entityBuilder.HasIndex(x => x.ChargingSessionId);
            entityBuilder.Property(x => x.EnergyCharged).HasPrecision(9, 3);
        });

        modelBuilder.Entity<SolarForecastHistoryEntry>(entityBuilder =>
        {
            entityBuilder.ToTable("SOLAR_FORECAST_HISTORY");
            entityBuilder.HasKey(x => x.Id).IsClustered(false);
            entityBuilder.Property(x => x.SysId).ValueGeneratedOnAdd();
            entityBuilder.HasIndex(x => x.SysId).IsClustered();
            entityBuilder.HasIndex(x => x.Date);
            entityBuilder.Property(x => x.Declination).HasPrecision(9, 3);
            entityBuilder.Property(x => x.Azimuth).HasPrecision(9, 3);
            entityBuilder.Property(x => x.Power).HasPrecision(9, 3);
            entityBuilder.Property(x => x.Damping).IsRequired();
            entityBuilder.Property(x => x.ForecastedEnergyToday).HasPrecision(9, 3);
            entityBuilder.Property(x => x.ActualEnergyToday).HasPrecision(9, 3);
            entityBuilder.Property(x => x.ForecastedEnergyTomorrow).HasPrecision(9, 3);
            entityBuilder.Property(x => x.ForecastedEnergyDayAfterTomorrow).HasPrecision(9, 3);
        });

        modelBuilder.Entity<DayAheadEnergyPricesEntry>(entityBuilder =>
        {
            entityBuilder.ToTable("DAY_AHEAD_ENERGY_PRICES");
            entityBuilder.HasKey(x => x.Id).IsClustered(false);
            entityBuilder.Property(x => x.SysId).ValueGeneratedOnAdd();
            entityBuilder.HasIndex(x => x.SysId).IsClustered();
            entityBuilder.HasIndex(x => x.From);
            entityBuilder.HasIndex(x => x.To);
            entityBuilder.Property(x => x.EuroPerMWh).HasPrecision(9, 3);
            entityBuilder.Property(x => x.ConsumptionTariffFormulaExpression).IsRequired();
            entityBuilder.Property(x => x.ConsumptionCentsPerKWh).HasPrecision(9, 3);
            entityBuilder.Property(x => x.InjectionTariffFormulaExpression).IsRequired();
            entityBuilder.Property(x => x.InjectionCentsPerKWh).HasPrecision(9, 3);
        });

        modelBuilder.Entity<EnergyInvoiceEntry>(entityBuilder =>
        {
            entityBuilder.ToTable("ENERGY_INVOICES");
            entityBuilder.HasKey(x => x.Id).IsClustered(false);
            entityBuilder.Property(x => x.SysId).ValueGeneratedOnAdd();
            entityBuilder.HasIndex(x => x.SysId).IsClustered();
            entityBuilder.Property(x => x.ElectricityAmount).HasPrecision(9, 2);
            entityBuilder.Property(x => x.GasAmount).HasPrecision(9, 2);
        });

        modelBuilder.Entity<CarChargesHistoryEntry>(entityBuilder =>
        {
            entityBuilder.ToTable("CAR_CHARGES_HISTORY");
            entityBuilder.HasKey(x => x.Id).IsClustered(false);
            entityBuilder.Property(x => x.SysId).ValueGeneratedOnAdd();
            entityBuilder.HasIndex(x => x.SysId).IsClustered();
            entityBuilder.HasIndex(x => x.TessieId);
            entityBuilder.HasIndex(x => new { x.StartedAt, x.EndedAt });
            entityBuilder.Property(x => x.EnergyAdded).HasPrecision(9, 2);
            entityBuilder.Property(x => x.EnergyUsed).HasPrecision(9, 2);
        });

        modelBuilder.Entity<CarDrivesHistoryEntry>(entityBuilder =>
        {
            entityBuilder.ToTable("CAR_DRIVES_HISTORY");
            entityBuilder.HasKey(x => x.Id).IsClustered(false);
            entityBuilder.Property(x => x.SysId).ValueGeneratedOnAdd();
            entityBuilder.HasIndex(x => x.SysId).IsClustered();
            entityBuilder.HasIndex(x => x.TessieId);
            entityBuilder.HasIndex(x => new { x.StartedAt, x.EndedAt });
            entityBuilder.Property(x => x.EnergyUsed).HasPrecision(9, 2);
            entityBuilder.Property(x => x.AverageInsideTemperature).HasPrecision(9, 2);
            entityBuilder.Property(x => x.AverageOutsideTemperature).HasPrecision(9, 2);
        });
    }
}