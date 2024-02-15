using MediatR;
using MijnThuis.Contracts.Solar;

namespace MijnThuis.Dashboard.Web.Components;

public partial class SolarTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromMinutes(1));
    
    public bool IsReady { get; set; }
    public string Title { get; set; }
    public decimal CurrentPower { get; set; }
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
            CurrentPower = response.CurrentPower;
            LastDayEnergy = response.LastDayEnergy / 1000M;
            LastMonthEnergy = response.LastMonthEnergy / 1000M;
            BatteryLevel = response.BatteryLevel;
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
}