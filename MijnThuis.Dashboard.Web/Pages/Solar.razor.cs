using Microsoft.AspNetCore.Components;
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


        _options2.YAxisTicks = 10;
        _options2.YAxisRequireZeroPoint = true;

        _series2.Clear();
        _series2.Add(new ChartSeries() { Name = "Belgium", Data = new double[] { -40, -20, -25, -27, -46, -46, -48, -44, -15 } });
        //_series2.Add(new ChartSeries() { Name = "United States", Data = new double[] { 40, 20, 25, 27, 46, 46, 48, 44, 15 } });
        _series2.Add(new ChartSeries() { Name = "Germany", Data = new double[] { -19, -24, -35, -13, -28, -15, -13, -16, -40 } });
        _series2.Add(new ChartSeries() { Name = "Sweden", Data = new double[] { -8, -6, -11, -13, -4, -16, -10, -16, -20 } });

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