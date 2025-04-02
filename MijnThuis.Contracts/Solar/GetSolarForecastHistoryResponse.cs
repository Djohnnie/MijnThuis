namespace MijnThuis.Contracts.Solar;

public class GetSolarForecastHistoryResponse
{
    public List<SolarForecastHistoryEntry> Entries { get; set; } = new();
}

public class SolarForecastHistoryEntry
{
    public DateTime Date { get; set; }
    public decimal ForecastedEnergyToday { get; set; }
    public decimal ActualEnergyToday { get; set; }
    public decimal ForecastedEnergyTomorrow { get; set; }
    public decimal ForecastedEnergyDayAfterTomorrow { get; set; }
}