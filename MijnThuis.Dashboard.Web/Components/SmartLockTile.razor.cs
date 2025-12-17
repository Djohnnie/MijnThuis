using MijnThuis.Contracts.SmartLock;
using MijnThuis.Dashboard.Web.Components.Dialogs;
using MudBlazor;

namespace MijnThuis.Dashboard.Web.Components;

public partial class SmartLockTile
{
    private readonly IDialogService _dialogService;
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(15));
    private readonly string _pin;

    public bool IsReady { get; set; }
    public string State { get; set; }
    public string DoorState { get; set; }
    public int BatteryCharge { get; set; }
    public string History { get; set; }

    public bool UnlockPending { get; set; }

    public SmartLockTile(
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
            var response = await Mediator.Send(new GetSmartLockOverviewQuery());
            State = response.State;
            DoorState = response.DoorState;
            BatteryCharge = response.BatteryCharge;
            var historicEntry = response.ActivityLog.FirstOrDefault()?.Action;
            var historicEntryTimestamp = $"{response.ActivityLog.FirstOrDefault()?.Timestamp:dd/MM/yyyy HH:mm}";
            History = $"{historicEntry ?? "Geen activiteit"}{(string.IsNullOrEmpty(historicEntryTimestamp) ? "" : $" ({historicEntryTimestamp})")}";
            IsReady = true;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh smart lock data");
        }
    }

    public async Task UnlockCommand()
    {
        var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Medium };
        var dialogResult = await _dialogService.ShowAsync<PinCodeDialog>("Bevestigen met pincode", options);
        var result = await dialogResult.GetReturnValueAsync<string>();

        if (result == _pin)
        {
            UnlockPending = true;
            await InvokeAsync(StateHasChanged);

            await Mediator.Send(new UnlockSmartLockCommand());

            UnlockPending = false;

            await RefreshData();
        }
    }

    public void Dispose()
    {
        _periodicTimer.Dispose();
    }
}