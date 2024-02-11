using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Power;

namespace MijnThuis.Dashboard.Web.Components;

public partial class PowerTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(10));

    [Inject]
    private IMediator _mediator { get; set; }

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
            var response = await _mediator.Send(new GetPowerOverviewQuery());
            CurrentPower = response.CurrentPower;
            PowerPeak = response.PowerPeak / 1000M;
            EnergyToday = response.EnergyToday;
            EnergyThisMonth = response.EnergyThisMonth;
            IsReady = true;

            await InvokeAsync(StateHasChanged);
        }
        catch
        {
            // Nothing we can do...
        }
    }

    public async Task WakeOnLan()
    {
        await _mediator.Send(new WakeOnLanCommand());
    }
}