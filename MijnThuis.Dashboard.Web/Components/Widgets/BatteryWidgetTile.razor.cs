using ApexCharts;
using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Application.Solar.Queries;
using MijnThuis.Dashboard.Web.Model;
using MijnThuis.Dashboard.Web.Model.Charts;

namespace MijnThuis.Dashboard.Web.Components.Widgets;

public partial class BatteryWidgetTile
{
    [CascadingParameter]
    public NotifyingDarkMode DarkMode { get; set; }

    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromMinutes(15));
    private ApexChart<ChartDataEntry<string, decimal?>> _apexChart = null!;
    private ApexChartOptions<ChartDataEntry<string, decimal?>> _options { get; set; } = new();

    private ChartData1<string, decimal?> BatteryLevel { get; set; } = new();

    public BatteryWidgetTile()
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
        _options.Responsive = new List<Responsive<ChartDataEntry<string, decimal?>>>
        {
            new Responsive<ChartDataEntry<string, decimal?>>
            {
                Breakpoint = 700,
                Options = new ApexChartOptions<ChartDataEntry<string, decimal?>>
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
            OverwriteCategories = Enumerable.Range(0, 24 * 12 + 1).Select(x => new DateTime().AddMinutes(5 * x).Minute == 0 && new DateTime().AddMinutes(5 * x).Hour % 2 == 0 ? $"{new DateTime().AddMinutes(5 * x):HH:mm}" : "").ToList(),
            AxisTicks = new AxisTicks
            {
                Show = false
            }
        };
        _options.Yaxis = new List<YAxis>
        {
            new YAxis
            {
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
        _options.Colors = new List<string> { "#564CDD" };
        _options.Stroke = new Stroke
        {
            Curve = Curve.Straight,
            Width = 5
        };
        _options.Fill = new Fill
        {
            Type = new List<FillType> { FillType.Solid },
            Opacity = new Opacity(1)
        };

        BatteryLevel.Description = "Thuisbatterij: gezondheid";
        BatteryLevel.Series1Description = "Geregistreerde gezondheid";
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

            var response = await mediator.Send(new GetBatteryLevelTodayQuery());

            BatteryLevel.Clear();
            BatteryLevel.Series1.AddRange(response.Entries.Select(x => new ChartDataEntry<string, decimal?>
            {
                XValue = $"{x.Date:HH:mm}",
                YValue = x.LevelOfCharge
            }));

            await InvokeAsync(StateHasChanged);
            await Task.Delay(100);
            await _apexChart.UpdateSeriesAsync(true);
            await _apexChart.UpdateOptionsAsync(true, true, false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh graph data");
        }
    }

    public void Dispose()
    {
        _periodicTimer.Dispose();
    }
}