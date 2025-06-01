namespace MijnThuis.Integrations.Solar;

public class EnergyOverview
{
    public decimal Produced { get; set; }
    public decimal Consumed { get; set; }
    public decimal Purchased { get; set; }
    public decimal Injected { get; set; }
}

public class EnergyProduced
{
    public decimal LastDayEnergy { get; init; }
    public decimal LastMonthEnergy { get; init; }
}