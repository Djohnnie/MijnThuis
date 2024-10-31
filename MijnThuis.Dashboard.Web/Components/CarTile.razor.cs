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
    public string BatteryBar { get; set; }
    public int BatteryHealth { get; set; }
    public int RemainingRange { get; set; }
    public int TemperatureInside { get; set; }
    public int TemperatureOutside { get; set; }
    public bool IsCharging { get; set; }
    public string ChargingCurrent { get; set; }
    public string ChargingRange { get; set; }
    public string Charger1 { get; set; }
    public bool Charger1Available { get; set; }
    public string Charger2 { get; set; }
    public bool Charger2Available { get; set; }

    public bool LockPending { get; set; }
    public bool UnlockPending { get; set; }

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
            var response = await Mediator.Send(new GetCarOverviewQuery());
            IsLocked = response.IsLocked;
            BatteryLevel = response.BatteryLevel;
            BatteryBar = BatteryLevel switch
            {
                < 10 => Icons.Material.Filled.Battery0Bar,
                < 20 => Icons.Material.Filled.Battery1Bar,
                < 30 => Icons.Material.Filled.Battery2Bar,
                < 40 => Icons.Material.Filled.Battery3Bar,
                < 60 => Icons.Material.Filled.Battery4Bar,
                < 80 => Icons.Material.Filled.Battery5Bar,
                < 100 => Icons.Material.Filled.Battery6Bar,
                100 => Icons.Material.Filled.BatteryFull,
                _ => Icons.Material.Filled.Battery0Bar,
            };
            BatteryHealth = response.BatteryHealth;
            RemainingRange = response.RemainingRange;
            TemperatureInside = response.TemperatureInside;
            TemperatureOutside = response.TemperatureOutside;
            Charger1 = response.Charger1;
            Charger1Available = response.Charger1Available;
            Charger2 = response.Charger2;
            Charger2Available = response.Charger2Available;
            IsCharging = response.IsCharging;
            ChargingCurrent = response.ChargingCurrent;
            ChargingRange = response.ChargingRange;
            IsReady = true;

            Title = response.Address;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh car data");
        }
    }

    public async Task LockCommand()
    {
        LockPending = true;
        await InvokeAsync(StateHasChanged);

        var commandResult = await Mediator.Send(new LockCarCommand());

        LockPending = false;
        IsLocked = commandResult.Success;

        await InvokeAsync(StateHasChanged);
    }

    public async Task UnlockCommand()
    {
        bool? result = await Message.ShowAsync();
        if (result == true)
        {
            UnlockPending = true;
            await InvokeAsync(StateHasChanged);

            var commandResult = await Mediator.Send(new UnlockCarCommand());

            UnlockPending = false;
            IsLocked = !commandResult.Success;

            await InvokeAsync(StateHasChanged);
        }
    }

    public async Task PreheatCommand()
    {
        await Mediator.Send(new PreheatCarCommand());
        await RefreshData();
    }

    public async Task FartCommand()
    {
        await Mediator.Send(new CarFartCommand());
        await RefreshData();
    }

    public void Dispose()
    {
        _periodicTimer?.Dispose();
    }
}