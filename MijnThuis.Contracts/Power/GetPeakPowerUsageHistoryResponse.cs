namespace MijnThuis.Contracts.Power;

public class GetPeakPowerUsageHistoryResponse
{
    public List<MonthlyPowerPeak> Entries { get; set; } = new();
}

public class MonthlyPowerPeak
{
    public DateTime Date { get; set; }
    public decimal PowerPeak { get; set; }
}