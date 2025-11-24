using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Solar;
using MijnThuis.Dashboard.Web.Components.Dialogs;
using MudBlazor;

namespace MijnThuis.Dashboard.Web.Components;

public partial class SolarTile
{
    [Inject]
    protected NavigationManager NavigationManager { get; set; }

    private readonly IDialogService _dialogService;
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(3));
    private readonly string _pin;

    public bool IsReady { get; set; }
    public string Title { get; set; }
    public decimal CurrentSolarPower { get; set; }
    public decimal CurrentBatteryPower { get; set; }
    public decimal CurrentGridPower { get; set; }
    public string BatteryBar { get; set; }
    public int BatteryLevel { get; set; }
    public int BatteryHealth { get; set; }
    public decimal LastDayEnergy { get; set; }
    public decimal LastMonthEnergy { get; set; }
    public decimal SolarForecastToday { get; set; }
    public decimal SolarForecastTomorrow { get; set; }

    public SolarTile(
        IDialogService dialogService,
        IConfiguration configuration)
    {
        _dialogService = dialogService;
        _pin = configuration.GetValue<string>("PINCODE");
    }

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
            var response = await Mediator.Send(new GetSolarOverviewQuery());
            CurrentSolarPower = response.CurrentSolarPower;
            CurrentBatteryPower = response.CurrentBatteryPower;
            CurrentGridPower = response.CurrentGridPower;
            LastDayEnergy = response.LastDayEnergy;
            LastMonthEnergy = response.LastMonthEnergy;
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
            SolarForecastToday = response.SolarForecastToday;
            SolarForecastTomorrow = response.SolarForecastTomorrow;
            IsReady = true;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh solar data");
        }
    }

    public void SolarPowerCommand()
    {
        NavigationManager.NavigateTo($"chart/SolarProductionChart{new Uri(NavigationManager.Uri).Query}");
    }

    public void BatteryLevelCommand()
    {
        NavigationManager.NavigateTo($"chart/BatteryHistoryChart{new Uri(NavigationManager.Uri).Query}");
    }

    public void LastDayEnergyCommand()
    {
        NavigationManager.NavigateTo($"chart/SolarProductionHistoryChart{new Uri(NavigationManager.Uri).Query}");
    }

    public void LastMonthEnergyCommand()
    {
        NavigationManager.NavigateTo($"chart/SolarYearlyProductionChart{new Uri(NavigationManager.Uri).Query}");
    }

    public void SolarForecastTodayCommand()
    {
        NavigationManager.NavigateTo($"chart/SolarForecastVsActualChart{new Uri(NavigationManager.Uri).Query}");
    }

    public async Task ChargeBatteryForOneHourCommand()
    {
        await ChargeBattery(1);
    }

    public async Task ChargeBatteryForTwoHoursCommand()
    {
        await ChargeBattery(2);
    }

    public async Task ChargeBatteryForThreeHoursCommand()
    {
        await ChargeBattery(3);
    }

    public async Task ChargeBatteryForFourHoursCommand()
    {
        await ChargeBattery(4);
    }

    private async Task ChargeBattery(int hours)
    {
        var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Medium };
        var dialogResult = await _dialogService.ShowAsync<PinCodeDialog>("Bevestigen met pincode", options);
        var result = await dialogResult.GetReturnValueAsync<string>();

        if (result == _pin)
        {
            await Mediator.Send(new ChargeBatteryCommand { Duration = TimeSpan.FromHours(hours), Power = 2000 });

            await RefreshData();
        }
    }

    public async Task StopChargingBatteryCommand()
    {
        await Mediator.Send(new StopChargingBatteryCommand());

        await RefreshData();
    }

    public void MoreCommand()
    {
        NavigationManager.NavigateTo($"solar{new Uri(NavigationManager.Uri).Query}");
    }

    public void Dispose()
    {
        _periodicTimer.Dispose();
    }
}