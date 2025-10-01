using ApexCharts;
using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Application.Solar.Queries;
using MijnThuis.Dashboard.Web.Model;
using MijnThuis.Dashboard.Web.Model.Charts;

namespace MijnThuis.Dashboard.Web.Components.Charts;

public partial class BatteryHistoryChart
{
    [CascadingParameter]
    public NotifyingDarkMode DarkMode { get; set; }

    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromHours(1));
    private ApexChart<ChartDataEntry<string, int?>> _apexChart = null!;
    private ApexChartOptions<ChartDataEntry<string, int?>> _options { get; set; } = new();

    private ChartData1<string, int?> BatteryLevel { get; set; } = new();
    private DateTime _selectedDate = DateTime.Today;

    public BatteryHistoryChart()
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
            },
            Background = "#373740",
        };
        _options.Responsive = new List<Responsive<ChartDataEntry<string, int?>>>
        {
            new Responsive<ChartDataEntry<string, int?>>
            {
                Breakpoint = 700,
                Options = new ApexChartOptions<ChartDataEntry<string, int?>>
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
            OverwriteCategories = Enumerable.Range(0, 24 * 4).Select(x => new DateTime().AddMinutes(15 * x).Minute == 0 && new DateTime().AddMinutes(15 * x).Hour % 2 == 0 ? $"{new DateTime().AddMinutes(15 * x):HH:mm}" : "").ToList(),
        };
        _options.Yaxis = new List<YAxis>
        {
            new YAxis
            {
                DecimalsInFloat = 0,
                Min = 0,
                Max = 100,
                Labels = new YAxisLabels
                {
                    Formatter = @"function (value) { return value + ' %'; }"
                }
            }
        };
        _options.Theme = new Theme
        {
            Mode = Mode.Dark,
            Palette = PaletteType.Palette1
        };
        _options.Colors = new List<string> { "#95B6F8" };
        _options.Stroke = new Stroke
        {
            Curve = Curve.Smooth
        };
        _options.Fill = new Fill
        {
            Type = new List<FillType> { FillType.Solid },
            Opacity = new Opacity(1)
        };

        BatteryLevel.Description = "Thuisbatterij";
        BatteryLevel.Series1Description = "Laadstatus";
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

            var response = await mediator.Send(new GetBatteryLevelHistoryQuery
            {
                Date = _selectedDate
            });

            var entries = response.Entries.OrderBy(x => x.Date);

            BatteryLevel.Clear();
            BatteryLevel.Series1.AddRange(entries.Select(x => new ChartDataEntry<string, int?>
            {
                XValue = $"{x.Date:HH:mm}",
                YValue = x.LevelOfCharge
            }));

            await InvokeAsync(StateHasChanged);
            await Task.Delay(100);
            await _apexChart.UpdateSeriesAsync(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh graph data");
        }
    }

    private async Task NavigateBeforeLargeCommand()
    {
        _selectedDate = _selectedDate.AddMonths(-1);
        await RefreshData();
    }

    private async Task NavigateBeforeCommand()
    {
        _selectedDate = _selectedDate.AddDays(-1);
        await RefreshData();
    }

    private async Task NavigateNextCommand()
    {
        _selectedDate = _selectedDate.AddDays(1);
        await RefreshData();
    }

    private async Task NavigateNextLargeCommand()
    {
        _selectedDate = _selectedDate.AddMonths(1);
        await RefreshData();
    }

    public void Dispose()
    {
        _periodicTimer.Dispose();
    }
}