namespace MijnThuis.Integrations.Forecast;

public class ForecastOverview
{
    public int EstimatedWattHoursToday { get; set; }
    public int EstimatedWattHoursTomorrow { get; set; }
    public int EstimatedWattHoursDayAfterTomorrow { get; set; }
    public List<WattHourPeriod> WattHourPeriods { get; set; } = new List<WattHourPeriod>();
    public TimeSpan Sunrise { get; set; }
    public TimeSpan Sunset { get; set; }
}

public class WattHourPeriod
{
    public DateTime Timestamp { get; set; }
    public int WattHours { get; set; }
}