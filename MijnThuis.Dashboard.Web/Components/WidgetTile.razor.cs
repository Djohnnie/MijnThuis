using MediatR;
using MijnThuis.Contracts.Power;
using MijnThuis.Contracts.Solar;
using MudBlazor;

namespace MijnThuis.Dashboard.Web.Components;

public partial class WidgetTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromMinutes(1));

    public bool IsReady { get; set; }
    public int BatteryLevel { get; set; }
    public string BatteryBar { get; set; }
    public decimal CurrentPower { get; set; }
    public decimal CurrentSolarPower { get; set; }
    public decimal CurrentBatteryPower { get; set; }
    public decimal CurrentGridPower { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _ = RunTimer();

        await base.OnInitializedAsync();
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
            var mediator = ScopedServices.GetRequiredService<IMediator>();
            var powerResponse = await mediator.Send(new GetPowerOverviewQuery());
            var solarResponse = await mediator.Send(new GetSolarOverviewQuery());
            BatteryLevel = solarResponse.BatteryLevel;
            CurrentPower = powerResponse.CurrentConsumption;
            CurrentSolarPower = solarResponse.CurrentSolarPower;
            CurrentBatteryPower = solarResponse.CurrentBatteryPower;
            CurrentGridPower = solarResponse.CurrentGridPower;
            BatteryBar = BatteryLevel switch
            {
                < 10 => Icons.Material.Filled.Battery0Bar,
                < 20 => Icons.Material.Filled.Battery1Bar,
                < 30 => Icons.Material.Filled.Battery2Bar,
                < 40 => Icons.Material.Filled.Battery3Bar,
                < 60 => Icons.Material.Filled.Battery4Bar,
                < 80 => Icons.Material.Filled.Battery5Bar,
                < 100 => Icons.Material.Filled.Battery6Bar,
                100 => Icons.Material.Filled.BatteryFull,
                _ => Icons.Material.Filled.Battery0Bar,
            };
            IsReady = true;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            var logger = ScopedServices.GetRequiredService<ILogger<WidgetTile>>();
            logger.LogError(ex, "Failed to refresh solar data");
        }
    }

    public void Dispose()
    {
        _periodicTimer?.Dispose();
    }
}