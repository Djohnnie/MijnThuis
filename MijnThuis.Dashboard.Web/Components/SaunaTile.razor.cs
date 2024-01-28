using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Sauna;

namespace MijnThuis.Dashboard.Web.Components;

public partial class SaunaTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(10));

    [Inject]
    private IMediator _mediator { get; set; }

    public bool IsReady { get; set; }
    public string Title { get; set; }
    public string State { get; set; }
    public int InsideTemperature { get; set; }
    public int OutsideTemperature { get; set; }
    public decimal Power { get; set; }

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
        var response = await _mediator.Send(new GetSaunaOverviewQuery());
        State = response.State;
        InsideTemperature = response.InsideTemperature;
        OutsideTemperature = response.OutsideTemperature;
        Power = response.Power / 1000M;
        IsReady = true;

        await InvokeAsync(StateHasChanged);
    }
}