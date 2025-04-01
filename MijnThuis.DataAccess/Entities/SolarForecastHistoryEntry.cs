namespace MijnThuis.DataAccess.Entities;

public class SolarForecastHistoryEntry
{
    public Guid Id { get; set; }
    public long SysId { get; set; }
    public DateTime Date { get; set; }
    public decimal Declination { get; set; }
    public decimal Azimuth { get; set; }
    public decimal Power { get; set; }
    public bool Damping { get; set; }
    public decimal ForecastedEnergyToday { get; set; }
    public decimal ActualEnergyToday { get; set; }
    public decimal ForecastedEnergyTomorrow { get; set; }
    public decimal ForecastedEnergyDayAfterTomorrow { get; set; }
}