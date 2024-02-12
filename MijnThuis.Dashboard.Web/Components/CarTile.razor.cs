using MediatR;
using MijnThuis.Contracts.Car;
using MudBlazor;

namespace MijnThuis.Dashboard.Web.Components;

public partial class CarTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromMinutes(1));

    internal MudMessageBox Message { get; set; }

    public bool IsReady { get; set; }
    public string Title { get; set; }
    public bool IsLocked { get; set; }
    public int BatteryLevel { get; set; }
    public int RemainingRange { get; set; }
    public int TemperatureInside { get; set; }
    public int TemperatureOutside { get; set; }

    public bool LockPending { get; set; }
    public bool UnlockPending { get; set; }

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

            var response = await mediator.Send(new GetCarOverviewQuery());
            IsLocked = response.IsLocked;
            BatteryLevel = response.BatteryLevel;
            RemainingRange = response.RemainingRange;
            TemperatureInside = response.TemperatureInside;
            TemperatureOutside = response.TemperatureOutside;
            IsReady = true;

            //Title = "Huidige status van de auto";

            //if (response.IsCharging)
            //{
            //    Title = "De auto is aan het opladen";
            //}

            //if (response.IsPreconditioning)
            //{
            //    Title = "De auto is aan het voorverwarmen";
            //}

            Title = response.Address;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            var logger = ScopedServices.GetRequiredService<ILogger<CarTile>>();
            logger.LogError(ex, "Failed to refresh car data");
        }
    }

    public async Task LockCommand()
    {
        var mediator = ScopedServices.GetRequiredService<IMediator>();

        LockPending = true;
        await InvokeAsync(StateHasChanged);

        var commandResult = await mediator.Send(new LockCarCommand());

        LockPending = false;
        IsLocked = commandResult.Success;

        await InvokeAsync(StateHasChanged);
    }

    public async Task UnlockCommand()
    {
        bool? result = await Message.Show();
        if (result == true)
        {
            var mediator = ScopedServices.GetRequiredService<IMediator>();

            UnlockPending = true;
            await InvokeAsync(StateHasChanged);

            var commandResult = await mediator.Send(new UnlockCarCommand());

            UnlockPending = false;
            IsLocked = !commandResult.Success;

            await InvokeAsync(StateHasChanged);
        }
    }

    public async Task PreheatCommand()
    {
        var mediator = ScopedServices.GetRequiredService<IMediator>();
        await mediator.Send(new PreheatCarCommand());
        await RefreshData();
    }

    public async Task FartCommand()
    {
        var mediator = ScopedServices.GetRequiredService<IMediator>();
        await mediator.Send(new CarFartCommand());
        await RefreshData();
    }

    public void Dispose()
    {
        _periodicTimer?.Dispose();
    }
}