namespace MijnThuis.Integrations.Heating;

public class HeatingOverview
{
    public decimal RoomTemperature { get; set; }
    public decimal OutdoorTemperature { get; set; }
    public decimal NextSetpoint { get; set; }
    public DateTime NextSwitchTime { get; set; }
}