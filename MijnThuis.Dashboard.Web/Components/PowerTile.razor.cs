using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Power;
using MijnThuis.Contracts.Solar;

namespace MijnThuis.Dashboard.Web.Components;

public partial class PowerTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(3));

    [Inject]
    protected NavigationManager NavigationManager { get; set; }
    public bool IsReady { get; set; }
    public string Title { get; set; }
    public decimal CurrentPower { get; set; }
    public decimal PowerPeak { get; set; }
    public decimal ImportToday { get; set; }
    public decimal ExportToday { get; set; }
    public decimal CostToday { get; set; }
    public decimal ImportThisMonth { get; set; }
    public decimal ExportThisMonth { get; set; }
    public decimal CostThisMonth { get; set; }
    public string CurrentPricePeriod { get; set; }
    public decimal CurrentConsumptionPrice { get; set; }
    public decimal CurrentInjectionPrice { get; set; }
    public string SelfConsumption { get; set; }
    public string SelfSufficiency { get; set; }

    public bool IsTvOn { get; set; }
    public bool ToggleTvPowerSwitchPending { get; set; }
    public bool IsBureauOn { get; set; }
    public bool ToggleBureauPowerSwitchPending { get; set; }
    public bool IsVijverOn { get; set; }
    public bool ToggleVijverPowerSwitchPending { get; set; }
    public bool IsTheFrameOn { get; set; }

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
            Title = response.Description;
            CurrentPower = response.CurrentConsumption;
            PowerPeak = response.PowerPeak / 1000M;
            ImportToday = response.ImportToday;
            ExportToday = response.ExportToday;
            CostToday = response.CostToday;
            ImportThisMonth = response.ImportThisMonth;
            ExportThisMonth = response.ExportThisMonth;
            CostThisMonth = response.CostThisMonth;
            CurrentPricePeriod = response.CurrentPricePeriod;
            CurrentConsumptionPrice = response.CurrentConsumptionPrice;
            CurrentInjectionPrice = response.CurrentInjectionPrice;
            SelfConsumption = $"{Math.Round(selfConsumption.SelfConsumptionToday):F0}% - {Math.Round(selfConsumption.SelfConsumptionThisMonth):F0}% - {Math.Round(selfConsumption.SelfConsumptionThisYear):F0}%";
            SelfSufficiency = $"{Math.Round(selfConsumption.SelfSufficiencyToday):F0}% - {Math.Round(selfConsumption.SelfSufficiencyThisMonth):F0}% - {Math.Round(selfConsumption.SelfSufficiencyThisYear):F0}%";
            IsTvOn = response.IsTvOn;
            IsBureauOn = response.IsBureauOn;
            IsVijverOn = response.IsVijverOn;
            IsTheFrameOn = response.IsTheFrameOn;
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

    public async Task ToggleTheFrame()
    {
        await Mediator.Send(new SetTheFrameCommand { TurnOn = !IsTheFrameOn });

        await RefreshData();
    }

    public async Task WakeOnLan()
    {
        await Mediator.Send(new WakeOnLanCommand());
    }

    public void MoreCommand()
    {
        NavigationManager.NavigateTo($"power{new Uri(NavigationManager.Uri).Query}");
    }

    public void CurrentPowerCommand()
    {
        NavigationManager.NavigateTo($"chart/EnergyUsageChart{new Uri(NavigationManager.Uri).Query}");
    }

    public void PowerPeakCommand()
    {
        NavigationManager.NavigateTo($"chart/PeakPowerUsageChart{new Uri(NavigationManager.Uri).Query}");
    }

    public void ConsumptionPriceCommand()
    {
        NavigationManager.NavigateTo($"chart/DayAheadEnergyPriceChart{new Uri(NavigationManager.Uri).Query}");
    }

    public void SelfConsumptionCommand()
    {
        NavigationManager.NavigateTo($"chart/SolarSelfConsumptionChart{new Uri(NavigationManager.Uri).Query}");
    }

    public void Dispose()
    {
        _periodicTimer.Dispose();
    }
}