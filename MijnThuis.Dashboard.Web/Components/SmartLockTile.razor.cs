using MijnThuis.Contracts.SmartLock;

namespace MijnThuis.Dashboard.Web.Components;

public partial class SmartLockTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(15));

    public bool IsReady { get; set; }
    public string State { get; set; }
    public string DoorState { get; set; }
    public int BatteryCharge { get; set; }
    public string History { get; set; }

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
            History = $"{(response.ActivityLog.FirstOrDefault()?.Action ?? "No activity")} ({response.ActivityLog.FirstOrDefault()?.Timestamp:dd/MM/yyyy HH:mm})";
            IsReady = true;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh smart lock data");
        }
    }

    public void Dispose()
    {
        _periodicTimer.Dispose();
    }
}