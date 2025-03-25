using MijnThuis.Contracts.Power;
using MijnThuis.Contracts.Solar;

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
    public string SelfConsumption { get; set; }
    public string SelfSufficiency { get; set; }

    public bool IsTvOn { get; set; }
    public bool ToggleTvPowerSwitchPending { get; set; }
    public bool IsBureauOn { get; set; }
    public bool ToggleBureauPowerSwitchPending { get; set; }
    public bool IsVijverOn { get; set; }
    public bool ToggleVijverPowerSwitchPending { get; set; }

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
            try
            {
                await RefreshData();
            }
            catch (ObjectDisposedException)
            {
                _periodicTimer.Dispose();
                break;
            }
        }
    }

    private async Task RefreshData()
    {
        try
        {
            var response = await Mediator.Send(new GetPowerOverviewQuery());
            var selfConsumption = await Mediator.Send(new GetSolarSelfConsumptionQuery { Date = DateTime.Today });
            CurrentPower = response.CurrentConsumption;
            PowerPeak = response.PowerPeak / 1000M;
            EnergyToday = response.EnergyToday;
            EnergyThisMonth = response.EnergyThisMonth;
            SelfConsumption = $"{selfConsumption.SelfConsumptionToday:F0}% - {selfConsumption.SelfConsumptionThisMonth:F0}% - {selfConsumption.SelfConsumptionThisYear:F0}%";
            SelfSufficiency = $"{selfConsumption.SelfSufficiencyToday:F0}% - {selfConsumption.SelfSufficiencyThisMonth:F0}% - {selfConsumption.SelfSufficiencyThisYear:F0}%";
            IsTvOn = response.IsTvOn;
            IsBureauOn = response.IsBureauOn;
            IsVijverOn = response.IsVijverOn;
            IsReady = true;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh power data");
        }
    }

    public async Task ToggleTvPowerSwitchCommand()
    {
        ToggleTvPowerSwitchPending = true;
        await InvokeAsync(StateHasChanged);

        var commandResult = await Mediator.Send(new SetTvPowerSwitchCommand { IsOn = !IsTvOn });

        ToggleTvPowerSwitchPending = false;

        await RefreshData();
    }

    public async Task ToggleBureauPowerSwitchCommand()
    {
        ToggleBureauPowerSwitchPending = true;
        await InvokeAsync(StateHasChanged);

        var commandResult = await Mediator.Send(new SetBureauPowerSwitchCommand { IsOn = !IsBureauOn });

        ToggleBureauPowerSwitchPending = false;

        await RefreshData();
    }

    public async Task ToggleVijverPowerSwitchCommand()
    {
        ToggleVijverPowerSwitchPending = true;
        await InvokeAsync(StateHasChanged);

        var commandResult = await Mediator.Send(new SetVijverPowerSwitchCommand { IsOn = !IsVijverOn });

        ToggleVijverPowerSwitchPending = false;

        await RefreshData();
    }

    public async Task WakeOnLan()
    {
        await Mediator.Send(new WakeOnLanCommand());
    }

    public void Dispose()
    {
        _periodicTimer.Dispose();
    }
}