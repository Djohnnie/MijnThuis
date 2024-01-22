using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Car;

namespace MijnThuis.Dashboard.Web.Components;

public partial class CarTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(10));

    [Inject]
    private IMediator _mediator { get; set; }

    public int BatteryLevel { get; set; }
    public int RemainingRange { get; set; }
    public int TemperatureInside { get; set; }
    public int TemperatureOutside { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _ = RunTimer();

        await base.OnInitializedAsync();
    }

    private async Task RunTimer()
    {
        while (await _periodicTimer.WaitForNextTickAsync())
        {
            var response = await _mediator.Send(new GetCarOverviewQuery());
            BatteryLevel = response.BatteryLevel;
            RemainingRange = response.RemainingRange;
            TemperatureInside = response.TemperatureInside;
            TemperatureOutside = response.TemperatureOutside;

            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        _periodicTimer?.Dispose();
    }
}