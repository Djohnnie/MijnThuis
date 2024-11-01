namespace MijnThuis.Integrations.Forecast;

public class ForecastOverview
{
    public int EstimatedWattHoursToday { get; set; }
    public int EstimatedWattHoursTomorrow { get; set; }
    public DateTime Sunrise { get; set; }
    public DateTime Sunset { get; set; }
}