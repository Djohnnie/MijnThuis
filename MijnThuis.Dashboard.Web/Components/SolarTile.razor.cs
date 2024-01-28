using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Solar;

namespace MijnThuis.Dashboard.Web.Components;

public partial class SolarTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(10));

    [Inject]
    private IMediator _mediator { get; set; }

    public bool IsReady { get; set; }
    public string Title { get; set; }
    public decimal CurrentPower { get; set; }
    public int BatteryLevel { get; set; }
    public decimal LastDayEnergy { get; set; }
    public decimal LastMonthEnergy { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _ = RunTimer();

        await base.OnInitializedAsync();
    }

    private async Task RunTimer()
    {
        while (await _periodicTimer.WaitForNextTickAsync())
        {
            await RefreshData();
        }
    }

    private async Task RefreshData()
    {
        var response = await _mediator.Send(new GetSolarOverviewQuery());
        CurrentPower = response.CurrentPower;
        LastDayEnergy = response.LastDayEnergy / 1000M;
        LastMonthEnergy = response.LastMonthEnergy / 1000M;
        BatteryLevel = (int)response.BatteryLevel;
        IsReady = true;

        await InvokeAsync(StateHasChanged);
    }
}