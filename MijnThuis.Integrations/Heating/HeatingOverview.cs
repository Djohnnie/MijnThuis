namespace MijnThuis.Integrations.Heating;

public class HeatingOverview
{
    public string Mode { get; set; }
    public decimal RoomTemperature { get; set; }
    public decimal Setpoint { get; set; }
    public decimal OutdoorTemperature { get; set; }
    public decimal NextSetpoint { get; set; }
    public DateTime NextSwitchTime { get; set; }
}