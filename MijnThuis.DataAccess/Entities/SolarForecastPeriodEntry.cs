namespace MijnThuis.DataAccess.Entities;

public class SolarForecastPeriodEntry
{
    public Guid Id { get; set; }
    public long SysId { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime DataFetched { get; set; }
    public decimal ForecastedEnergy { get; set; }
    public decimal ActualEnergy { get; set; }
}