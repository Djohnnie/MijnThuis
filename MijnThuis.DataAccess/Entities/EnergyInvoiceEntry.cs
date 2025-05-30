namespace MijnThuis.DataAccess.Entities;

public class EnergyInvoiceEntry
{
    public Guid Id { get; set; }
    public long SysId { get; set; }
    public DateTime Date { get; set; }
    public decimal ElectricityAmount { get; set; }
    public decimal GasAmount { get; set; }
}