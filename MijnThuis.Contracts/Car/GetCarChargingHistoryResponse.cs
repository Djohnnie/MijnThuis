namespace MijnThuis.Contracts.Car;

public class GetCarChargingHistoryResponse
{
    public List<CarChargingEnergyHistoryEntry> Entries { get; set; }
}

public class CarChargingEnergyHistoryEntry
{
    public DateTime Date { get; set; }
    public decimal EnergyCharged { get; set; }
}