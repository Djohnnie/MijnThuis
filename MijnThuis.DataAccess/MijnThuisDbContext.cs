using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MijnThuis.DataAccess.Entities;

namespace MijnThuis.DataAccess;

public class MijnThuisDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public DbSet<SolarEnergyHistoryEntry> SolarEnergyHistory { get; set; }
    public DbSet<SolarPowerHistoryEntry> SolarPowerHistory { get; set; }
    public DbSet<BatteryEnergyHistoryEntry> BatteryEnergyHistory { get; set; }

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
        modelBuilder.Entity<SolarEnergyHistoryEntry>(entityBuilder =>
        {
            entityBuilder.ToTable("SOLAR_ENERGY_HISTORY");
            entityBuilder.HasKey(x => x.Id).IsClustered(false);
            entityBuilder.Property(x => x.SysId).ValueGeneratedOnAdd();
            entityBuilder.HasIndex(x => x.SysId).IsClustered();
            entityBuilder.HasIndex(x => x.Date);
        });

        modelBuilder.Entity<SolarPowerHistoryEntry>(entityBuilder =>
        {
            entityBuilder.ToTable("SOLAR_POWER_HISTORY");
            entityBuilder.HasKey(x => x.Id).IsClustered(false);
            entityBuilder.Property(x => x.SysId).ValueGeneratedOnAdd();
            entityBuilder.HasIndex(x => x.SysId).IsClustered();
            entityBuilder.HasIndex(x => x.Date);
        });

        modelBuilder.Entity<BatteryEnergyHistoryEntry>(entityBuilder =>
        {
            entityBuilder.ToTable("BATTERY_ENERGY_HISTORY");
            entityBuilder.HasKey(x => x.Id).IsClustered(false);
            entityBuilder.Property(x => x.SysId).ValueGeneratedOnAdd();
            entityBuilder.HasIndex(x => x.SysId).IsClustered();
            entityBuilder.HasIndex(x => x.Date);
        });
    }
}