using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Car;
using MudBlazor;

namespace MijnThuis.Dashboard.Web.Components;

public partial class CarTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(10));

    [Inject]
    private IMediator _mediator { get; set; }

    internal MudMessageBox Message { get; set; }

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

        Title = "Huidige status van de auto";

        if (response.IsCharging)
        {
            Title = "De auto is aan het opladen";
        }

        if (response.IsPreconditioning)
        {
            Title = "De auto is aan het voorverwarmen";
        }

        await InvokeAsync(StateHasChanged);
    }

    public async Task LockCommand()
    {
        await _mediator.Send(new LockCarCommand());
        //await RefreshData();
        IsLocked = true;
    }

    public async Task UnlockCommand()
    {
        bool? result = await Message.Show();
        if (result == true)
        {
            await _mediator.Send(new UnlockCarCommand());
            IsLocked = false;
            //await RefreshData();
        }
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