namespace MijnThuis.Contracts.Sauna;

public class GetSaunaOverviewResponse
{
    public string State { get; set; }
    public int InsideTemperature { get; set; }
    public int OutsideTemperature { get; set; }
    public int Power { get; set; }
}