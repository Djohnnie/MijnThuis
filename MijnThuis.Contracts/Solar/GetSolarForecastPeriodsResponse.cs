namespace MijnThuis.Contracts.Solar;

public class GetSolarForecastPeriodsResponse
{
    public List<SolarForecastPeriod> Periods { get; set; } = new();
}

public class SolarForecastPeriod
{
    public DateTime Timestamp { get; set; }
    public decimal? ForecastedEnergy { get; set; }
    public decimal? ActualEnergy { get; set; }
}