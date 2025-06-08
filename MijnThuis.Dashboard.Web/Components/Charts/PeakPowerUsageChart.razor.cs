using ApexCharts;
using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Power;
using MijnThuis.Dashboard.Web.Model;
using MijnThuis.Dashboard.Web.Model.Charts;

namespace MijnThuis.Dashboard.Web.Components.Charts;

public partial class PeakPowerUsageChart
{
    [CascadingParameter]
    public NotifyingDarkMode DarkMode { get; set; }

    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromHours(1));
    private ApexChart<ChartDataEntry<string, decimal>> _apexChart = null!;
    private ApexChartOptions<ChartDataEntry<string, decimal>> _options { get; set; } = new();

    private ChartData1<string, decimal> PeakPowerUsage { get; set; } = new();
    private int _selectedYear = DateTime.Today.Year;

    public string TitleDescription { get; set; }

    public PeakPowerUsageChart()
    {
        _options.Chart = new Chart
        {
            Toolbar = new Toolbar
            {
                Show = false
            },
            Zoom = new Zoom
            {
                Enabled = false
            }
        };
        _options.Responsive = new List<Responsive<ChartDataEntry<string, decimal>>>
        {
            new Responsive<ChartDataEntry<string, decimal>>
            {
                Breakpoint = 700,
                Options = new ApexChartOptions<ChartDataEntry<string, decimal>>
                {
                    Legend = new Legend
                    {
                        Show = false
                    },
                    Xaxis = new XAxis
                    {
                        Labels = new XAxisLabels
                        {
                            Show = false
                        }
                    },
                },
            }
        };
        _options.Xaxis = new XAxis
        {
            Type = XAxisType.Category,
        };
        _options.Yaxis = new List<YAxis>
        {
            new YAxis
            {
                DecimalsInFloat = 0,
                Labels = new YAxisLabels
                {
                    Formatter = @"function (value) { return value + ' kW'; }"
                }
            }
        };
        _options.Theme = new Theme
        {
            Mode = Mode.Dark,
            Palette = PaletteType.Palette1
        };
        _options.Fill = new Fill
        {
            Type = new List<FillType> { FillType.Solid },
            Opacity = new Opacity(1, 1, 1)
        };

        PeakPowerUsage.Description = "Elektriciteit: Piekverbruik";
        PeakPowerUsage.Series1Description = "Maandelijks piekverbruik";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (DarkMode != null)
            {
                _options.Chart.Background = DarkMode.IsDarkMode ? "#373740" : "#FFFFFF";
                _options.Theme.Mode = DarkMode.IsDarkMode ? Mode.Dark : Mode.Light;
                await _apexChart.UpdateOptionsAsync(true, false, false);

                DarkMode.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(DarkMode.IsDarkMode))
                    {
                        _options.Chart.Background = DarkMode.IsDarkMode ? "#373740" : "#FFFFFF";
                        _options.Theme.Mode = DarkMode.IsDarkMode ? Mode.Dark : Mode.Light;
                        _ = _apexChart.UpdateOptionsAsync(true, false, false);
                    }
                };
            }

            _ = RunTimer();
        }

        await base.OnAfterRenderAsync(firstRender);
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
            using var scope = ServiceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var from = new DateTime(2000, 1, 1);
            var to = DateTime.Today;

            var response = await mediator.Send(new GetPeakPowerUsageHistoryQuery
            {
                Year = _selectedYear
            });

            var entries = response.Entries;

            PeakPowerUsage.Clear();
            PeakPowerUsage.Series1.AddRange(entries.Select(x => new ChartDataEntry<string, decimal>
            {
                XValue = $"{x.Date:MMMM yyyy}",
                YValue = Math.Round(x.PowerPeak, 2)
            }));

            TitleDescription = $"Maandelijks piekvermogen in {_selectedYear}";

            await InvokeAsync(StateHasChanged);
            await Task.Delay(100);
            await _apexChart.UpdateSeriesAsync(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh graph data");
        }
    }

    private string GetColor(ChartDataEntry<string, decimal> entry)
    {
        if (entry.YValue > 2.5M)
        {
            return "#FBB550";
        }

        return "#B0D8FD";
    }

    private async Task NavigateBeforeCommand()
    {
        _selectedYear--;

        await RefreshData();
    }

    private async Task NavigateNextCommand()
    {
        _selectedYear++;

        await RefreshData();
    }

    public void Dispose()
    {
        _periodicTimer.Dispose();
    }
}