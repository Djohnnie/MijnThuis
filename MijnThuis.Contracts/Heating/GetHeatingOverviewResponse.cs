namespace MijnThuis.Contracts.Heating;

public class GetHeatingOverviewResponse
{
    public string Mode { get; set; }
    public decimal RoomTemperature { get; set; }
    public decimal Setpoint { get; set; }
    public decimal OutdoorTemperature { get; set; }
    public decimal NextSetpoint { get; set; }
    public DateTime NextSwitchTime { get; set; }
    public decimal GasUsageToday { get; set; }
    public decimal GasUsageTodayKwh { get; set; }
    public decimal GasUsageThisMonth { get; set; }
    public decimal GasUsageThisMonthKwh { get; set; }
}