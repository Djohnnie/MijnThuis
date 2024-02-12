using MediatR;
using MijnThuis.Contracts.Power;

namespace MijnThuis.Dashboard.Web.Components;

public partial class PowerTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(10));

    public bool IsReady { get; set; }
    public string Title { get; set; }
    public int CurrentPower { get; set; }
    public decimal PowerPeak { get; set; }
    public decimal EnergyToday { get; set; }
    public decimal EnergyThisMonth { get; set; }

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

            var response = await mediator.Send(new GetPowerOverviewQuery());
            CurrentPower = response.CurrentPower;
            PowerPeak = response.PowerPeak / 1000M;
            EnergyToday = response.EnergyToday;
            EnergyThisMonth = response.EnergyThisMonth;
            IsReady = true;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            var logger = ScopedServices.GetRequiredService<ILogger<PowerTile>>();
            logger.LogError(ex, "Failed to refresh power data");
        }
    }

    public async Task WakeOnLan()
    {
        var mediator = ScopedServices.GetRequiredService<IMediator>();
        await mediator.Send(new WakeOnLanCommand());
    }
}