using MediatR;
using MijnThuis.Contracts.Car;
using MijnThuis.Contracts.Power;

namespace MijnThuis.Dashboard.Web.Components;

public partial class PowerTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(5));

    public bool IsReady { get; set; }
    public string Title { get; set; }
    public decimal CurrentPower { get; set; }
    public decimal PowerPeak { get; set; }
    public decimal EnergyToday { get; set; }
    public decimal EnergyThisMonth { get; set; }
    public bool IsTvOn { get; set; }
    public bool ToggleTvPowerSwitchPending { get; set; }
    public bool IsBureauOn { get; set; }
    public bool ToggleBureauPowerSwitchPending { get; set; }

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
            CurrentPower = response.CurrentConsumption;
            PowerPeak = response.PowerPeak / 1000M;
            EnergyToday = response.EnergyToday;
            EnergyThisMonth = response.EnergyThisMonth;
            IsTvOn = response.IsTvOn;
            IsBureauOn = response.IsBureauOn;
            IsReady = true;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            var logger = ScopedServices.GetRequiredService<ILogger<PowerTile>>();
            logger.LogError(ex, "Failed to refresh power data");
        }
    }

    public async Task ToggleTvPowerSwitchCommand()
    {
        var mediator = ScopedServices.GetRequiredService<IMediator>();

        ToggleTvPowerSwitchPending = true;
        await InvokeAsync(StateHasChanged);

        var commandResult = await mediator.Send(new SetTvPowerSwitchCommand { IsOn = !IsTvOn });

        ToggleTvPowerSwitchPending = false;

        await RefreshData();
    }

    public async Task ToggleBureauPowerSwitchCommand()
    {
        var mediator = ScopedServices.GetRequiredService<IMediator>();

        ToggleBureauPowerSwitchPending = true;
        await InvokeAsync(StateHasChanged);

        var commandResult = await mediator.Send(new SetBureauPowerSwitchCommand { IsOn = !IsBureauOn });

        ToggleBureauPowerSwitchPending = false;

        await RefreshData();
    }

    public async Task WakeOnLan()
    {
        var mediator = ScopedServices.GetRequiredService<IMediator>();
        await mediator.Send(new WakeOnLanCommand());
    }
}