using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Airconditioning;

namespace MijnThuis.Dashboard.Web.Components;

public partial class AirconditioningTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(10));

    public bool IsReady { get; set; }
    public bool IsOn { get; set; }
    public decimal RoomTemperature { get; set; }
    public decimal TargetTemperature { get; set; }
    public string Mode { get; set; }
    public string FanSpeed { get; set; }

    public bool TogglePowerPending { get; set; }
    public bool SetTargetTemperature16Pending { get; set; }
    public bool SetTargetTemperature22Pending { get; set; }
    public bool SetTargetTemperature25Pending { get; set; }
    public bool IncreaseTargetTemperaturePending { get; set; }
    public bool DecreaseTargetTemperaturePending { get; set; }

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
            var response = await Mediator.Send(new GetAirconditioningOverviewQuery());
            IsOn = response.IsOn;
            RoomTemperature = response.RoomTemperature;
            TargetTemperature = response.TargetTemperature;
            Mode = response.Mode;
            FanSpeed = response.FanSpeed;
            IsReady = true;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh airconditioning data");
        }
    }

    protected async Task TogglePowerCommand()
    {
        TogglePowerPending = true;
        await InvokeAsync(StateHasChanged);

        if (IsOn)
        {
            await Mediator.Send(new TurnOffAirconditioningCommand());
        }
        else
        {
            await Mediator.Send(new TurnOnAirconditioningCommand());
        }

        TogglePowerPending = false;

        await RefreshData();
    }

    protected async Task SetTargetTemperature16Command()
    {
        SetTargetTemperature16Pending = true;
        await InvokeAsync(StateHasChanged);

        await Mediator.Send(new SetTargetTemperature16AirconditioningCommand());

        SetTargetTemperature16Pending = false;

        await RefreshData();
    }

    protected async Task SetTargetTemperature22Command()
    {
        SetTargetTemperature22Pending = true;
        await InvokeAsync(StateHasChanged);

        await Mediator.Send(new SetTargetTemperature22AirconditioningCommand());

        SetTargetTemperature22Pending = false;

        await RefreshData();
    }

    protected async Task SetTargetTemperature25Command()
    {
        SetTargetTemperature25Pending = true;
        await InvokeAsync(StateHasChanged);

        await Mediator.Send(new SetTargetTemperature25AirconditioningCommand());

        SetTargetTemperature25Pending = false;

        await RefreshData();
    }

    protected async Task IncreaseTargetTemperatureCommand()
    {
        IncreaseTargetTemperaturePending = true;
        await InvokeAsync(StateHasChanged);

        await Mediator.Send(new IncreaseTargetTemperatureAirconditioningCommand());

        IncreaseTargetTemperaturePending = false;

        await RefreshData();
    }

    protected async Task DecreaseTargetTemperatureCommand()
    {
        DecreaseTargetTemperaturePending = true;
        await InvokeAsync(StateHasChanged);

        await Mediator.Send(new DecreaseTargetTemperatureAirconditioningCommand());

        DecreaseTargetTemperaturePending = false;

        await RefreshData();
    }

    public void Dispose()
    {
        _periodicTimer.Dispose();
    }
}
