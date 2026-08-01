namespace MijnThuis.Integrations.Airconditioning;

public class AirconditioningOverview
{
    public bool IsOn { get; set; }
    public decimal RoomTemperature { get; set; }
    public decimal TargetTemperature { get; set; }
    public string Mode { get; set; }
    public string FanSpeed { get; set; }
}
