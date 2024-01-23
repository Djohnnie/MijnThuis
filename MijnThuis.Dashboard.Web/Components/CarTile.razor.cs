using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Car;

namespace MijnThuis.Dashboard.Web.Components;

public partial class CarTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(10));

    [Inject]
    private IMediator _mediator { get; set; }

    public bool IsReady { get; set; }
    public string Title { get; set; }
    public bool IsLocked { get; set; }
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
            await RefreshData();
        }
    }

    private async Task RefreshData()
    {
        var response = await _mediator.Send(new GetCarOverviewQuery());
        IsLocked = response.IsLocked;
        BatteryLevel = response.BatteryLevel;
        RemainingRange = response.RemainingRange;
        TemperatureInside = response.TemperatureInside;
        TemperatureOutside = response.TemperatureOutside;
        IsReady = true;

        Title = response.IsPreconditioning ? "De auto is aan het voorverwarmen" : "Huidige status van de auto";

        await InvokeAsync(StateHasChanged);
    }

    public async Task LockCommand()
    {
        await _mediator.Send(new LockCarCommand());
        await RefreshData();
    }

    public async Task UnlockCommand()
    {
        await _mediator.Send(new UnlockCarCommand());
        await RefreshData();
    }

    public async Task PreheatCommand()
    {
        await _mediator.Send(new PreheatCarCommand());
        await RefreshData();
    }

    public async Task FartCommand()
    {
        await _mediator.Send(new CarFartCommand());
        await RefreshData();
    }

    public void Dispose()
    {
        _periodicTimer?.Dispose();
    }
}