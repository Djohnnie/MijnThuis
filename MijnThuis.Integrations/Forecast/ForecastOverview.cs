namespace MijnThuis.Integrations.Forecast;

public class ForecastOverview
{
    public int EstimatedWattHoursToday { get; set; }
    public int EstimatedWattHoursTomorrow { get; set; }
    public TimeSpan Sunrise { get; set; }
    public TimeSpan Sunset { get; set; }
}