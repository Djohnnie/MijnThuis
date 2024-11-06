using MediatR;
using MijnThuis.Contracts.Sauna;

namespace MijnThuis.Dashboard.Web.Components;

public partial class SaunaTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(15));

    public bool IsReady { get; set; }
    public string Title { get; set; }
    public string State { get; set; }
    public int InsideTemperature { get; set; }
    public int OutsideTemperature { get; set; }
    public decimal Power { get; set; }

    public bool StartSaunaPending { get; set; }
    public bool StartInfraredPending { get; set; }
    public bool StopSaunaPending { get; set; }

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
            var response = await Mediator.Send(new GetSaunaOverviewQuery());
            State = response.State;
            InsideTemperature = response.InsideTemperature;
            OutsideTemperature = response.OutsideTemperature;
            Power = response.Power;
            IsReady = true;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh sauna data");
        }
    }

    public async Task StartSaunaCommand()
    {
        StartSaunaPending = true;
        await InvokeAsync(StateHasChanged);

        await Mediator.Send(new StartSaunaCommand());

        StartSaunaPending = false;

        await RefreshData();
    }

    public async Task StartInfraredCommand()
    {
        StartInfraredPending = true;
        await InvokeAsync(StateHasChanged);

        await Mediator.Send(new StartInfraredCommand());

        StartInfraredPending = false;

        await RefreshData();
    }

    public async Task StopSaunaCommand()
    {
        StopSaunaPending = true;
        await InvokeAsync(StateHasChanged);

        await Mediator.Send(new StopSaunaCommand());

        StopSaunaPending = false;

        await RefreshData();
    }

    public void Dispose()
    {
        _periodicTimer?.Dispose();
    }
}