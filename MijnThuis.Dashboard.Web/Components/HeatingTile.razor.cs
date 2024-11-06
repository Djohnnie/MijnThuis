using MediatR;
using MijnThuis.Contracts.Heating;

namespace MijnThuis.Dashboard.Web.Components;

public partial class HeatingTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromMinutes(1));

    public bool IsReady { get; set; }
    public string Title { get; set; }
    public decimal RoomTemperature { get; set; }
    public decimal Setpoint { get; set; }
    public decimal OutdoorTemperature { get; set; }
    public string Status { get; set; }
    public string NextSetpoint { get; set; }
    public string NextSwitchTime { get; set; }
    public bool ScheduledHeatingPending { get; set; }
    public bool Manual22HeatingPending { get; set; }
    public bool Manual16HeatingPending { get; set; }
    public bool AntiFrostHeatingPending { get; set; }
    public bool TemporaryOverrideHeatingPending { get; set; }

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
            var response = await Mediator.Send(new GetHeatingOverviewQuery());
            RoomTemperature = response.RoomTemperature;
            Setpoint = response.Setpoint;
            OutdoorTemperature = response.OutdoorTemperature;
            Status = response.Mode;
            NextSetpoint = $"{response.NextSetpoint:F1}";
            NextSwitchTime = $"{response.NextSwitchTime:HH:mm}";
            IsReady = true;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh heating data");
        }
    }

    protected async Task SetScheduledHeatingCommand()
    {
        ScheduledHeatingPending = true;
        await InvokeAsync(StateHasChanged);

        await Mediator.Send(new SetScheduledHeatingCommand());

        ScheduledHeatingPending = false;

        await RefreshData();
    }

    protected async Task SetTemporaryOverrideHeatingCommand()
    {
        TemporaryOverrideHeatingPending = true;
        await InvokeAsync(StateHasChanged);

        await Mediator.Send(new SetTemporaryOverride22HeatingCommand());

        TemporaryOverrideHeatingPending = false;

        await RefreshData();
    }

    protected async Task SetManual22HeatingCommand()
    {
        Manual22HeatingPending = true;
        await InvokeAsync(StateHasChanged);

        await Mediator.Send(new SetManual22HeatingCommand());

        Manual22HeatingPending = false;

        await RefreshData();
    }

    protected async Task SetManual16HeatingCommand()
    {
        Manual16HeatingPending = true;
        await InvokeAsync(StateHasChanged);

        await Mediator.Send(new SetManual16HeatingCommand());

        Manual16HeatingPending = false;

        await RefreshData();
    }

    protected async Task SetAntiFrostHeatingCommand()
    {
        AntiFrostHeatingPending = true;
        await InvokeAsync(StateHasChanged);

        await Mediator.Send(new SetAntiFrostHeatingCommand());

        AntiFrostHeatingPending = false;

        await RefreshData();
    }

    public void Dispose()
    {
        _periodicTimer?.Dispose();
    }
}