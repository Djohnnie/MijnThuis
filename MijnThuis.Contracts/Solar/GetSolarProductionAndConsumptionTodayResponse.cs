namespace MijnThuis.Contracts.Solar;

public class GetSolarProductionAndConsumptionTodayResponse
{
    public decimal Production { get; set; }
    public decimal ProductionToHome { get; set; }
    public decimal ProductionToBattery { get; set; }
    public decimal ProductionToGrid { get; set; }
    public decimal Consumption { get; set; }
    public decimal ConsumptionFromSolar { get; set; }
    public decimal ConsumptionFromBattery { get; set; }
    public decimal ConsumptionFromGridToBattery { get; set; }
    public decimal ConsumptionFromGrid { get; set; }
}