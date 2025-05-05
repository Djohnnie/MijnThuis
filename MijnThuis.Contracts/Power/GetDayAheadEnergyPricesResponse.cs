namespace MijnThuis.Contracts.Power;

public class GetDayAheadEnergyPricesResponse
{
    public List<DayAheadEnergyPrice> Entries { get; set; } = new();
}

public class DayAheadEnergyPrice
{
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
}