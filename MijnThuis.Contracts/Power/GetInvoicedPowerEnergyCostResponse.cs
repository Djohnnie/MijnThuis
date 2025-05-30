namespace MijnThuis.Contracts.Power;

public class GetInvoicedPowerEnergyCostResponse
{
    public List<EnergyInvoiceEntry> ThisYear { get; set; }
    public List<EnergyInvoiceEntry> LastYear { get; set; }
    public List<EnergyInvoiceEntry> AllYears { get; set; }
}

public class EnergyInvoiceEntry
{
    public DateTime Date { get; set; }
    public decimal ElectricityAmount { get; set; }
}