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
    private string[] XAxisLabels { get; set; }

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
        _options.DisableLegend = true;
        _options.InterpolationOption = InterpolationOption.Straight;

        XAxisLabels = data.Entries.Select(x =>
        {
            var label = string.Empty;

            return label;
        }).ToArray();

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