using ApexCharts;
using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Solar;
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

    private ChartData2<string, decimal?> BatteryLevel { get; set; } = new();

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
            Background = "#32333C",
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
            OverwriteCategories = Enumerable.Range(0, 24 * 4 + 1).Select(x => new DateTime().AddMinutes(15 * x).Minute == 0 && new DateTime().AddMinutes(15 * x).Hour % 2 == 0 ? $"{new DateTime().AddMinutes(15 * x):HH:mm}" : "").ToList(),
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
        _options.Colors = new List<string> { "#564CDD", "#5DE799" };
        _options.Stroke = new Stroke
        {
            Curve = Curve.Smooth,
            Width = new List<int> { 3, 1 }
        };
        _options.Fill = new Fill
        {
            Type = new List<FillType> { FillType.Solid },
            Opacity = new Opacity(1)
        };

        BatteryLevel.Description = "Thuisbatterij";
        BatteryLevel.Series1Description = "Laadtoestand vandaag";
        BatteryLevel.Series2Description = "Berekende gezondheid vandaag";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (DarkMode != null)
            {
                _options.Chart.Background = DarkMode.IsDarkMode ? "#32333C" : "#FFFFFF";
                _options.Theme.Mode = DarkMode.IsDarkMode ? Mode.Dark : Mode.Light;
                await _apexChart.UpdateOptionsAsync(true, false, false);

                DarkMode.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(DarkMode.IsDarkMode))
                    {
                        _options.Chart.Background = DarkMode.IsDarkMode ? "#32333C" : "#FFFFFF";
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
        await Task.Delay(Random.Shared.Next(1000, 5000));
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
            BatteryLevel.Series2.AddRange(response.Entries.Select(x => new ChartDataEntry<string, decimal?>
            {
                XValue = $"{x.Date:HH:mm}",
                YValue = Math.Min(x.StateOfHealth ?? 0, 100)
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