namespace MijnThuis.DataAccess.Entities;

public class DayAheadEnergyPricesEntry
{
    public Guid Id { get; set; }
    public long SysId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal EuroPerMWh { get; set; }
    public string ConsumptionTariffFormulaExpression { get; set; }
    public decimal ConsumptionCentsPerKWh { get; set; }
    public string InjectionTariffFormulaExpression { get; set; }
    public decimal InjectionCentsPerKWh { get; set; }
}