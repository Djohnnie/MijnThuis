namespace MijnThuis.Contracts.Heating;

public class GetInvoicedGasCostResponse
{
    public List<GasInvoiceEntry> ThisYear { get; set; }
    public List<GasInvoiceEntry> LastYear { get; set; }
    public List<GasInvoiceEntry> AllYears { get; set; }
}

public class GasInvoiceEntry
{
    public DateTime Date { get; set; }
    public decimal GasAmount { get; set; }
}
