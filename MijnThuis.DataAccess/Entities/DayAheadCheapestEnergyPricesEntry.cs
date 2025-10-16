namespace MijnThuis.DataAccess.Entities;

public class DayAheadCheapestEnergyPricesEntry
{
    public Guid Id { get; set; }
    public long SysId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int Order { get; set; }
    public decimal EuroPerMWh { get; set; }
}