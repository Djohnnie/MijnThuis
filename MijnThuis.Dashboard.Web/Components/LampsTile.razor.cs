using MediatR;
using MijnThuis.Contracts.Lamps;

namespace MijnThuis.Dashboard.Web.Components;

public partial class LampsTile
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
            var response = await Mediator.Send(new GetLampsOverviewQuery());
            //CurrentPower = response.CurrentConsumption;
            //PowerPeak = response.PowerPeak / 1000M;
            //EnergyToday = response.EnergyToday;
            //EnergyThisMonth = response.EnergyThisMonth;
            //IsTvOn = response.IsTvOn;
            //IsBureauOn = response.IsBureauOn;
            IsReady = true;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh lights data");
        }
    }
}