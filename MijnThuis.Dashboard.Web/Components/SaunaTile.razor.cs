using MediatR;
using MijnThuis.Contracts.Sauna;

namespace MijnThuis.Dashboard.Web.Components;

public partial class SaunaTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(60));

    public bool IsReady { get; set; }
    public string Title { get; set; }
    public string State { get; set; }
    public int InsideTemperature { get; set; }
    public int OutsideTemperature { get; set; }
    public decimal Power { get; set; }

    public bool StartSaunaPending { get; set; }
    public bool StartInfraredPending { get; set; }
    public bool StopSaunaPending { get; set; }

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

            var response = await mediator.Send(new GetSaunaOverviewQuery());
            State = response.State;
            InsideTemperature = response.InsideTemperature;
            OutsideTemperature = response.OutsideTemperature;
            Power = response.Power;
            IsReady = true;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            var logger = ScopedServices.GetRequiredService<ILogger<SaunaTile>>();
            logger.LogError(ex, "Failed to refresh sauna data");
        }
    }

    public async Task StartSaunaCommand()
    {
        var mediator = ScopedServices.GetRequiredService<IMediator>();

        StartSaunaPending = true;
        await InvokeAsync(StateHasChanged);

        await mediator.Send(new StartSaunaCommand());

        StartSaunaPending = false;

        await RefreshData();
    }

    public async Task StartInfraredCommand()
    {
        var mediator = ScopedServices.GetRequiredService<IMediator>();

        StartInfraredPending = true;
        await InvokeAsync(StateHasChanged);

        await mediator.Send(new StartInfraredCommand());

        StartInfraredPending = false;

        await RefreshData();
    }

    public async Task StopSaunaCommand()
    {
        var mediator = ScopedServices.GetRequiredService<IMediator>();

        StopSaunaPending = true;
        await InvokeAsync(StateHasChanged);

        await mediator.Send(new StopSaunaCommand());

        StopSaunaPending = false;

        await RefreshData();
    }
}