using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MijnThuis.DataAccess.Entities;

namespace MijnThuis.DataAccess;

public class MijnThuisDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public DbSet<SolarHistoryEntry> SolarHistory { get; set; }

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
        modelBuilder.Entity<SolarHistoryEntry>(entityBuilder =>
        {
            entityBuilder.ToTable("SOLAR_HISTORY");
            entityBuilder.HasKey(x => x.Id).IsClustered(false);
            entityBuilder.Property(x => x.SysId).ValueGeneratedOnAdd();
            entityBuilder.HasIndex(x => x.SysId).IsClustered();
        });
    }
}