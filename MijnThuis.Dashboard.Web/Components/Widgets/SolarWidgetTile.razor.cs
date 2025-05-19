using MijnThuis.Contracts.Car;
using MijnThuis.Contracts.Solar;

namespace MijnThuis.Dashboard.Web.Components.Widgets;

public partial class SolarWidgetTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(2));

    public bool IsReady { get; set; }

    public decimal CurrentSolarPower { get; set; }
    public decimal CurrentBatteryPower { get; set; }
    public decimal CurrentGridPower { get; set; }
    public decimal CurrentHomePower { get; set; }
    public bool IsCarCharging { get; set; }
    public decimal CurrentCarPower { get; set; }
    public int BatteryLevel { get; set; }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _ = RunTimer();
        }

        return base.OnAfterRenderAsync(firstRender);
    }

    private async Task RunTimer()
    {
        await RefreshData();

        while (await _periodicTimer.WaitForNextTickAsync())
        {
            await RefreshData();
        }
    }

    private async Task RefreshData()
    {
        try
        {
            var solarResponse = await Mediator.Send(new GetSolarOverviewQuery());
            var carResponse = await Mediator.Send(new GetCarOverviewQuery());

            CurrentSolarPower = solarResponse.CurrentSolarPower;
            CurrentBatteryPower = solarResponse.CurrentBatteryPower;
            CurrentGridPower = solarResponse.CurrentGridPower;
            IsCarCharging = carResponse.IsCharging;
            CurrentCarPower = IsCarCharging ? (16 * 230 / 1000M) : 0M;
            CurrentHomePower = IsCarCharging ? solarResponse.CurrentConsumptionPower - (16 * 230 / 1000M) : solarResponse.CurrentConsumptionPower;
            BatteryLevel = solarResponse.BatteryLevel;
            IsReady = true;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh solar data");
        }
    }

    public void Dispose()
    {
        _periodicTimer?.Dispose();
    }
}