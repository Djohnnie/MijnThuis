using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Car;
using MijnThuis.Contracts.Heating;
using MijnThuis.Contracts.Power;
using MijnThuis.Contracts.Sauna;
using MijnThuis.Contracts.SmartLock;
using MijnThuis.Contracts.Solar;
using MijnThuis.Dashboard.Web.Components.Dialogs;
using MudBlazor;
using System.Security.Cryptography;
using System.Text;

namespace MijnThuis.Dashboard.Web.Pages;

public partial class Home : IDisposable
{
    private readonly IDialogService _dialogService;
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(5));
    private readonly string _pin;

    [Inject]
    protected NavigationManager NavigationManager { get; set; }

    // Power & switches
    public bool PowerReady { get; set; }
    public decimal CurrentPower { get; set; }
    public decimal PowerPeak { get; set; }
    public decimal ImportToday { get; set; }
    public decimal ExportToday { get; set; }
    public decimal CostToday { get; set; }
    public decimal CostThisMonth { get; set; }
    public string CurrentPricePeriod { get; set; }
    public decimal CurrentConsumptionPrice { get; set; }
    public decimal CurrentInjectionPrice { get; set; }
    public string SelfConsumption { get; set; }
    public string SelfSufficiency { get; set; }
    public bool SwitchesReady { get; set; }
    public bool IsTvOn { get; set; }
    public bool IsBureauOn { get; set; }
    public bool IsVijverOn { get; set; }
    public bool IsTheFrameOn { get; set; }

    // Solar & battery
    public bool SolarReady { get; set; }
    public decimal CurrentSolarPower { get; set; }
    public decimal CurrentBatteryPower { get; set; }
    public decimal CurrentGridPower { get; set; }
    public string BatterySolarBar { get; set; } = Icons.Material.Filled.Battery0Bar;
    public int BatteryLevel { get; set; }
    public int BatteryHealth { get; set; }
    public decimal LastDayEnergy { get; set; }
    public decimal LastMonthEnergy { get; set; }
    public decimal SolarForecastToday { get; set; }
    public decimal SolarForecastTomorrow { get; set; }

    // Car
    public bool CarReady { get; set; }
    public bool IsCarLocked { get; set; }
    public int CarBatteryLevel { get; set; }
    public string CarBatteryBar { get; set; } = Icons.Material.Filled.Battery0Bar;
    public int CarBatteryHealth { get; set; }
    public int CarRemainingRange { get; set; }
    public int CarTempInside { get; set; }
    public int CarTempOutside { get; set; }
    public bool IsCarCharging { get; set; }
    public bool IsCarChargingManuallyAt8 { get; set; }
    public bool IsCarChargingManuallyAt16 { get; set; }
    public string CarChargingCurrent { get; set; }
    public string CarCharger1 { get; set; }
    public bool CarCharger1Available { get; set; }
    public string CarCharger2 { get; set; }
    public bool CarCharger2Available { get; set; }
    public bool CarLockPending { get; set; }
    public bool CarUnlockPending { get; set; }

    // Heating
    public bool HeatingReady { get; set; }
    public decimal RoomTemperature { get; set; }
    public decimal HeatingSetpoint { get; set; }
    public decimal OutdoorTemperature { get; set; }
    public string HeatingStatus { get; set; }
    public string HeatingNextSetpoint { get; set; }
    public string HeatingNextSwitchTime { get; set; }
    public string GasUsageToday { get; set; }
    public string GasUsageThisMonth { get; set; }
    public bool ScheduledHeatingPending { get; set; }
    public bool TemporaryOverrideHeatingPending { get; set; }
    public bool Manual23HeatingPending { get; set; }
    public bool Manual16HeatingPending { get; set; }
    public bool AntiFrostHeatingPending { get; set; }

    // Smart lock
    public bool LockReady { get; set; }
    public string LockState { get; set; }
    public string LockDoorState { get; set; }
    public string LockHistory { get; set; }
    public bool LockUnlockPending { get; set; }

    // Sauna
    public bool SaunaReady { get; set; }
    public string SaunaState { get; set; }
    public int SaunaInsideTemp { get; set; }
    public decimal SaunaPower { get; set; }
    public bool StartSaunaPending { get; set; }
    public bool StartInfraredPending { get; set; }
    public bool StopSaunaPending { get; set; }

    public Home(IDialogService dialogService, IConfiguration configuration)
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
        await Task.WhenAll(
            RefreshPowerData(),
            RefreshSolarData(),
            RefreshCarData(),
            RefreshHeatingData(),
            RefreshLockData(),
            RefreshSaunaData());

        await InvokeAsync(StateHasChanged);
    }

    private async Task RefreshPowerData()
    {
        try
        {
            var response = await Mediator.Send(new GetPowerOverviewQuery());
            var selfConsumption = await Mediator.Send(new GetSolarSelfConsumptionQuery { Date = DateTime.Today });
            CurrentPower = response.CurrentConsumption;
            PowerPeak = response.PowerPeak / 1000M;
            ImportToday = response.ImportToday;
            ExportToday = response.ExportToday;
            CostToday = response.CostToday;
            CostThisMonth = response.CostThisMonth;
            CurrentPricePeriod = response.CurrentPricePeriod;
            CurrentConsumptionPrice = response.CurrentConsumptionPrice;
            CurrentInjectionPrice = response.CurrentInjectionPrice;
            SelfConsumption = $"{Math.Round(selfConsumption.SelfConsumptionToday):F0}%";
            SelfSufficiency = $"{Math.Round(selfConsumption.SelfSufficiencyToday):F0}%";
            PowerReady = true;

            var switchResponse = await Mediator.Send(new GetPowerSwitchOverviewQuery());
            IsTvOn = switchResponse.IsTvOn;
            IsBureauOn = switchResponse.IsBureauOn;
            IsVijverOn = switchResponse.IsVijverOn;
            IsTheFrameOn = switchResponse.IsTheFrameOn;
            SwitchesReady = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh power data");
        }
    }

    private async Task RefreshSolarData()
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
            BatterySolarBar = BatteryLevel switch
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
            SolarReady = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh solar data");
        }
    }

    private async Task RefreshCarData()
    {
        try
        {
            var response = await Mediator.Send(new GetCarOverviewQuery());
            IsCarLocked = response.IsLocked;
            CarBatteryLevel = response.BatteryLevel;
            CarBatteryBar = CarBatteryLevel switch
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
            CarBatteryHealth = response.BatteryHealth;
            CarRemainingRange = response.RemainingRange;
            CarTempInside = response.TemperatureInside;
            CarTempOutside = response.TemperatureOutside;
            CarCharger1 = response.Charger1;
            CarCharger1Available = response.Charger1Available;
            CarCharger2 = response.Charger2;
            CarCharger2Available = response.Charger2Available;
            IsCarCharging = response.IsCharging;
            IsCarChargingManuallyAt8 = response.IsChargingManually && response.ChargingAmps == 8;
            IsCarChargingManuallyAt16 = response.IsChargingManually && response.ChargingAmps == 16;
            CarChargingCurrent = response.ChargingCurrent;
            CarReady = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh car data");
        }
    }

    private async Task RefreshHeatingData()
    {
        try
        {
            var response = await Mediator.Send(new GetHeatingOverviewQuery());
            RoomTemperature = response.RoomTemperature;
            HeatingSetpoint = response.Setpoint;
            OutdoorTemperature = response.OutdoorTemperature;
            HeatingStatus = response.Mode;
            HeatingNextSetpoint = $"{response.NextSetpoint:F0}";
            HeatingNextSwitchTime = $"{response.NextSwitchTime:HH:mm}";
            GasUsageToday = $"{response.GasUsageToday:F1} m³";
            GasUsageThisMonth = $"{response.GasUsageThisMonth:F1} m³";
            HeatingReady = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh heating data");
        }
    }

    private async Task RefreshLockData()
    {
        try
        {
            var response = await Mediator.Send(new GetSmartLockOverviewQuery());
            LockState = response.State;
            LockDoorState = response.DoorState;
            var historicEntry = response.ActivityLog.FirstOrDefault()?.Action;
            var historicTimestamp = $"{response.ActivityLog.FirstOrDefault()?.Timestamp:dd/MM/yyyy HH:mm}";
            LockHistory = $"{historicEntry ?? "Geen activiteit"}{(string.IsNullOrEmpty(historicTimestamp) ? "" : $" ({historicTimestamp})")}";
            LockReady = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh smart lock data");
        }
    }

    private async Task RefreshSaunaData()
    {
        try
        {
            var response = await Mediator.Send(new GetSaunaOverviewQuery());
            SaunaState = response.State;
            SaunaInsideTemp = response.InsideTemperature;
            SaunaPower = response.Power;
            SaunaReady = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh sauna data");
        }
    }

    // --- Car actions ---

    public async Task LockCarAction()
    {
        CarLockPending = true;
        await InvokeAsync(StateHasChanged);
        var result = await Mediator.Send(new LockCarCommand());
        CarLockPending = false;
        IsCarLocked = result.Success;
        await InvokeAsync(StateHasChanged);
    }

    public async Task UnlockCarAction()
    {
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium };
        var dialogResult = await _dialogService.ShowAsync<PinCodeDialog>("Bevestigen met pincode", options);
        var pin = await dialogResult.GetReturnValueAsync<string>();
        CarUnlockPending = true;
        await InvokeAsync(StateHasChanged);
        var result = await Mediator.Send(new UnlockCarCommand { Pin = pin });
        CarUnlockPending = false;
        IsCarLocked = !result.Success;
        await InvokeAsync(StateHasChanged);
    }

    public async Task PreheatCarAction()
    {
        await Mediator.Send(new PreheatCarCommand());
        await RefreshCarData();
        await InvokeAsync(StateHasChanged);
    }

    public async Task CarFartAction()
    {
        await Mediator.Send(new CarFartCommand());
    }

    public async Task StartCarChargingAt8Action()
    {
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium };
        var dialogResult = await _dialogService.ShowAsync<PinCodeDialog>("Bevestigen met pincode", options);
        var pin = await dialogResult.GetReturnValueAsync<string>();
        await Mediator.Send(new SetManualCarChargeCommand { Pin = pin, IsEnabled = true, ChargeAmps = 8 });
        await RefreshCarData();
        await InvokeAsync(StateHasChanged);
    }

    public async Task StartCarChargingAt16Action()
    {
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium };
        var dialogResult = await _dialogService.ShowAsync<PinCodeDialog>("Bevestigen met pincode", options);
        var pin = await dialogResult.GetReturnValueAsync<string>();
        await Mediator.Send(new SetManualCarChargeCommand { Pin = pin, IsEnabled = true, ChargeAmps = 16 });
        await RefreshCarData();
        await InvokeAsync(StateHasChanged);
    }

    public async Task StopCarChargingAction()
    {
        await Mediator.Send(new SetManualCarChargeCommand { IsEnabled = false, ChargeAmps = 0 });
        await RefreshCarData();
        await InvokeAsync(StateHasChanged);
    }

    // --- Heating actions ---

    public async Task SetScheduledHeatingAction()
    {
        ScheduledHeatingPending = true;
        await InvokeAsync(StateHasChanged);
        await Mediator.Send(new SetScheduledHeatingCommand());
        ScheduledHeatingPending = false;
        await RefreshHeatingData();
        await InvokeAsync(StateHasChanged);
    }

    public async Task SetTemporaryOverrideHeatingAction()
    {
        TemporaryOverrideHeatingPending = true;
        await InvokeAsync(StateHasChanged);
        await Mediator.Send(new SetTemporaryOverride23HeatingCommand());
        TemporaryOverrideHeatingPending = false;
        await RefreshHeatingData();
        await InvokeAsync(StateHasChanged);
    }

    public async Task SetManual23HeatingAction()
    {
        Manual23HeatingPending = true;
        await InvokeAsync(StateHasChanged);
        await Mediator.Send(new SetManual23HeatingCommand());
        Manual23HeatingPending = false;
        await RefreshHeatingData();
        await InvokeAsync(StateHasChanged);
    }

    public async Task SetManual16HeatingAction()
    {
        Manual16HeatingPending = true;
        await InvokeAsync(StateHasChanged);
        await Mediator.Send(new SetManual16HeatingCommand());
        Manual16HeatingPending = false;
        await RefreshHeatingData();
        await InvokeAsync(StateHasChanged);
    }

    public async Task SetAntiFrostHeatingAction()
    {
        AntiFrostHeatingPending = true;
        await InvokeAsync(StateHasChanged);
        await Mediator.Send(new SetAntiFrostHeatingCommand());
        AntiFrostHeatingPending = false;
        await RefreshHeatingData();
        await InvokeAsync(StateHasChanged);
    }

    // --- Smart lock actions ---

    public async Task UnlockDoorAction()
    {
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium };
        var dialogResult = await _dialogService.ShowAsync<PinCodeDialog>("Bevestigen met pincode", options);
        var result = await dialogResult.GetReturnValueAsync<string>();

        if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(result ?? ""), Encoding.UTF8.GetBytes(_pin ?? "")))
        {
            LockUnlockPending = true;
            await InvokeAsync(StateHasChanged);
            await Mediator.Send(new UnlockSmartLockCommand());
            LockUnlockPending = false;
            await RefreshLockData();
            await InvokeAsync(StateHasChanged);
        }
    }

    // --- Sauna actions ---

    public async Task StartSaunaAction()
    {
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium };
        var dialogResult = await _dialogService.ShowAsync<PinCodeDialog>("Bevestigen met pincode", options);
        var result = await dialogResult.GetReturnValueAsync<string>();

        if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(result ?? ""), Encoding.UTF8.GetBytes(_pin ?? "")))
        {
            StartSaunaPending = true;
            await InvokeAsync(StateHasChanged);
            await Mediator.Send(new StartSaunaCommand());
            StartSaunaPending = false;
            await RefreshSaunaData();
            await InvokeAsync(StateHasChanged);
        }
    }

    public async Task StartInfraredAction()
    {
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium };
        var dialogResult = await _dialogService.ShowAsync<PinCodeDialog>("Bevestigen met pincode", options);
        var result = await dialogResult.GetReturnValueAsync<string>();

        if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(result ?? ""), Encoding.UTF8.GetBytes(_pin ?? "")))
        {
            StartInfraredPending = true;
            await InvokeAsync(StateHasChanged);
            await Mediator.Send(new StartInfraredCommand());
            StartInfraredPending = false;
            await RefreshSaunaData();
            await InvokeAsync(StateHasChanged);
        }
    }

    public async Task StopSaunaAction()
    {
        StopSaunaPending = true;
        await InvokeAsync(StateHasChanged);
        await Mediator.Send(new StopSaunaCommand());
        StopSaunaPending = false;
        await RefreshSaunaData();
        await InvokeAsync(StateHasChanged);
    }

    // --- Switch actions ---

    public async Task ToggleTvAction()
    {
        await Mediator.Send(new SetTvPowerSwitchCommand { IsOn = !IsTvOn });
        await RefreshPowerData();
        await InvokeAsync(StateHasChanged);
    }

    public async Task ToggleBureauAction()
    {
        await Mediator.Send(new SetBureauPowerSwitchCommand { IsOn = !IsBureauOn });
        await RefreshPowerData();
        await InvokeAsync(StateHasChanged);
    }

    public async Task ToggleVijverAction()
    {
        await Mediator.Send(new SetVijverPowerSwitchCommand { IsOn = !IsVijverOn });
        await RefreshPowerData();
        await InvokeAsync(StateHasChanged);
    }

    public async Task ToggleTheFrameAction()
    {
        await Mediator.Send(new SetTheFrameCommand { TurnOn = !IsTheFrameOn });
        await RefreshPowerData();
        await InvokeAsync(StateHasChanged);
    }

    // --- Power actions ---

    public async Task WakeOnLanAction()
    {
        await Mediator.Send(new WakeOnLanCommand());
    }

    // --- Solar battery actions ---

    public async Task ChargeBatteryFor1HourAction() => await ChargeBattery(1);
    public async Task ChargeBatteryFor2HoursAction() => await ChargeBattery(2);
    public async Task ChargeBatteryFor3HoursAction() => await ChargeBattery(3);
    public async Task ChargeBatteryFor4HoursAction() => await ChargeBattery(4);

    private async Task ChargeBattery(int hours)
    {
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium };
        var dialogResult = await _dialogService.ShowAsync<PinCodeDialog>("Bevestigen met pincode", options);
        var result = await dialogResult.GetReturnValueAsync<string>();

        if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(result ?? ""), Encoding.UTF8.GetBytes(_pin ?? "")))
        {
            await Mediator.Send(new ChargeBatteryCommand { Duration = TimeSpan.FromHours(hours), Power = 2000 });
            await RefreshSolarData();
            await InvokeAsync(StateHasChanged);
        }
    }

    public async Task StopBatteryChargingAction()
    {
        await Mediator.Send(new StopChargingBatteryCommand());
        await RefreshSolarData();
        await InvokeAsync(StateHasChanged);
    }

    // --- Computed style properties ---

    public string BatterySolarIconStyle => $"color: {(BatteryLevel > 30 ? "#4caf50" : "#ff9800")}; font-size: 1.5rem;";
    public string BatterySolarLargeIconStyle => $"font-size: 1.7rem; color: {(BatteryLevel > 30 ? "#4caf50" : "#ff9800")};";
    public string GridPowerIconStyle => $"color: {(CurrentGridPower > 0 ? "#f44336" : "#4caf50")}; font-size: 1.5rem;";
    public string PowerPeakIconStyle => $"color: {(PowerPeak >= 2.5M ? "#f44336" : "#90caf9")}; font-size: 1.5rem;";
    public string CarBatteryIconStyle => $"font-size: 1.7rem; color: {(CarBatteryLevel < 20 ? "#f44336" : CarBatteryLevel < 50 ? "#ff9800" : "#4caf50")};";
    public string RoomTempStyle => $"color: {(RoomTemperature < HeatingSetpoint ? "#ff9800" : "#4caf50")};";

    public void Dispose()
    {
        _periodicTimer.Dispose();
    }
}
