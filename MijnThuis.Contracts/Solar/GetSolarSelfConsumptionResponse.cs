namespace MijnThuis.Contracts.Solar;

public class GetSolarSelfConsumptionResponse
{
    public decimal SelfConsumptionToday { get; set; }
    public decimal SelfConsumptionThisMonth { get; set; }
    public decimal SelfConsumptionThisYear { get; set; }
    public decimal SelfSufficiencyToday { get; set; }
    public decimal SelfSufficiencyThisMonth { get; set; }
    public decimal SelfSufficiencyThisYear { get; set; }
}