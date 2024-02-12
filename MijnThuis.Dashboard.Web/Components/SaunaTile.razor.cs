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
            Power = response.Power / 1000M;
            IsReady = true;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            var logger = ScopedServices.GetRequiredService<ILogger<SaunaTile>>();
            logger.LogError(ex, "Failed to refresh sauna data");
        }
    }
}