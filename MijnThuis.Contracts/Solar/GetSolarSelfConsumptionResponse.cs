namespace MijnThuis.Contracts.Solar;

public class GetSolarSelfConsumptionResponse
{
    public decimal SelfConsumptionToday { get; set; }
    public decimal SelfConsumptionThisMonth { get; set; }
    public decimal SelfConsumptionThisYear { get; set; }
    public decimal SelfSufficiencyToday { get; set; }
    public decimal SelfSufficiencyThisMonth { get; set; }
    public decimal SelfSufficiencyThisYear { get; set; }

    public List<SolarSelfConsumptionEntry> Entries { get; set; } = new();
}

public class SolarSelfConsumptionEntry
{
    public DateTime Date { get; set; }
    public decimal SelfConsumption { get; set; }
    public decimal SelfSufficiency { get; set; }
}