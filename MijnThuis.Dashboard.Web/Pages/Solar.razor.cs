using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Solar;
using MijnThuis.Integrations.Solar;
using MudBlazor;

namespace MijnThuis.Dashboard.Web.Pages;

public partial class Solar
{
    [Inject]
    protected NavigationManager NavigationManager { get; set; }

    private readonly List<ChartSeries> _series = new();
    private readonly ChartOptions _options = new();

    private readonly List<ChartSeries> _series2 = new();
    private readonly ChartOptions _options2 = new();

    private readonly List<ChartSeries> _series3 = new();
    private readonly ChartOptions _options3 = new();

    private readonly List<ChartSeries> _series4 = new();
    private readonly ChartOptions _options4 = new();

    private readonly List<ChartSeries> _series5 = new();
    private readonly ChartOptions _options5 = new();

    private string[] XAxisLabels { get; set; } = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep" };

    protected override async Task OnInitializedAsync()
    {
        await RefreshData(StorageDataRange.Today);

        await base.OnInitializedAsync();
    }

    private async Task RefreshData(StorageDataRange dataRange)
    {
        var solarService = ScopedServices.GetRequiredService<ISolarService>();

        var data = await solarService.GetStorageData(dataRange);

        _series.Clear();
        _series.Add(new ChartSeries { Name = "Battery", Data = data.Entries.Select(x => (double)x.ChargeState).ToArray() });

        _options.YAxisTicks = 10;
        _options.YAxisLines = true;
        _options.ShowLegend = false;
        _options.InterpolationOption = InterpolationOption.Straight;

        //_series2.Clear();
        //_series2.Add(new ChartSeries() { Name = "Belgium", Data = new double[] { -40, -20, -25, -27, -46, -46, -48, -44, -15 } });
        ////_series2.Add(new ChartSeries() { Name = "United States", Data = new double[] { 40, 20, 25, 27, 46, 46, 48, 44, 15 } });
        //_series2.Add(new ChartSeries() { Name = "Germany", Data = new double[] { -19, -24, -35, -13, -28, -15, -13, -16, -40 } });
        //_series2.Add(new ChartSeries() { Name = "Sweden", Data = new double[] { -8, -6, -11, -13, -4, -16, -10, -16, -20 } });

        var mediator = ScopedServices.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetSolarEnergyHistoryQuery
        {
            From = new DateTime(2024, 1, 1),
            To = new DateTime(2024, 12, 31),
            Unit = EnergyHistoryUnit.Month
        });

        _series2.Clear();
        _series2.Add(new ChartSeries { Name = "Production to home", Data = response.Entries.Select(x => (double)x.ProductionToHome).ToArray() });
        _series2.Add(new ChartSeries { Name = "Production to battery", Data = response.Entries.Select(x => (double)x.ProductionToBattery).ToArray() });
        _series2.Add(new ChartSeries { Name = "Production to grid", Data = response.Entries.Select(x => (double)x.ProductionToGrid).ToArray() });

        _series3.Clear();
        _series3.Add(new ChartSeries { Name = "Consumption from solar", Data = response.Entries.Select(x => (double)x.ConsumptionFromSolar).ToArray() });
        _series3.Add(new ChartSeries { Name = "Consumption from battery", Data = response.Entries.Select(x => (double)x.ConsumptionFromBattery).ToArray() });
        _series3.Add(new ChartSeries { Name = "Consumption from grid", Data = response.Entries.Select(x => (double)x.ConsumptionFromGrid).ToArray() });

        _series4.Clear();
        _series4.Add(new ChartSeries { Name = "Import", Data = response.Entries.Select(x => (double)x.Import).ToArray() });

        _series5.Clear();
        _series5.Add(new ChartSeries { Name = "Export", Data = response.Entries.Select(x => (double)x.Export).ToArray() });

        await InvokeAsync(StateHasChanged);
    }

    public void BackCommand()
    {
        NavigationManager.NavigateTo($"/{new Uri(NavigationManager.Uri).Query}");
    }

    public async Task BatteryTodayCommand()
    {
        await RefreshData(StorageDataRange.Today);
    }

    public async Task BatteryThreeDaysCommand()
    {
        await RefreshData(StorageDataRange.ThreeDays);
    }

    public async Task BatteryWeekCommand()
    {
        await RefreshData(StorageDataRange.Week);
    }

    public async Task BatteryMonthCommand()
    {
        await RefreshData(StorageDataRange.Month);
    }
}