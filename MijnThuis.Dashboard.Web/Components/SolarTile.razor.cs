using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Solar;
using MudBlazor;

namespace MijnThuis.Dashboard.Web.Components;

public partial class SolarTile
{
    [Inject]
    protected NavigationManager NavigationManager { get; set; }

    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(5));

    public bool IsReady { get; set; }
    public string Title { get; set; }
    public decimal CurrentSolarPower { get; set; }
    public decimal CurrentBatteryPower { get; set; }
    public decimal CurrentGridPower { get; set; }
    public string BatteryBar { get; set; }
    public int BatteryLevel { get; set; }
    public int BatteryHealth { get; set; }
    public decimal LastDayEnergy { get; set; }
    public decimal LastMonthEnergy { get; set; }

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

            var response = await mediator.Send(new GetSolarOverviewQuery());
            CurrentSolarPower = response.CurrentSolarPower;
            CurrentBatteryPower = response.CurrentBatteryPower;
            CurrentGridPower = response.CurrentGridPower;
            LastDayEnergy = response.LastDayEnergy;
            LastMonthEnergy = response.LastMonthEnergy;
            BatteryLevel = response.BatteryLevel;
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
            BatteryHealth = response.BatteryHealth;
            IsReady = true;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            var logger = ScopedServices.GetRequiredService<ILogger<SolarTile>>();
            logger.LogError(ex, "Failed to refresh solar data");
        }
    }

    public void MoreCommand()
    {
        NavigationManager.NavigateTo($"solar{new Uri(NavigationManager.Uri).Query}");
    }
}