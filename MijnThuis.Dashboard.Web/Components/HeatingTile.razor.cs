using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Heating;

namespace MijnThuis.Dashboard.Web.Components;

public partial class HeatingTile
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(10));

    [Inject]
    private IMediator _mediator { get; set; }

    public bool IsReady { get; set; }
    public string Title { get; set; }
    public decimal RoomTemperature { get; set; }
    public decimal OutdoorTemperature { get; set; }
    public string NextSetpoint { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _ = RunTimer();

        await base.OnInitializedAsync();
    }

    private async Task RunTimer()
    {
        while (await _periodicTimer.WaitForNextTickAsync())
        {
            await RefreshData();
        }
    }

    private async Task RefreshData()
    {
        var response = await _mediator.Send(new GetHeatingOverviewQuery());
        RoomTemperature = response.RoomTemperature;
        OutdoorTemperature = response.OutdoorTemperature;
        NextSetpoint = $"{response.NextSetpoint:F1} °C om {response.NextSwitchTime:HH:mm}";
        IsReady = true;

        await InvokeAsync(StateHasChanged);
    }
}