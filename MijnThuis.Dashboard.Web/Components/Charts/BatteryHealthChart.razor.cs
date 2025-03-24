using ApexCharts;
using MediatR;
using MijnThuis.Contracts.Solar;
using MijnThuis.Dashboard.Web.Model.Charts;

namespace MijnThuis.Dashboard.Web.Components.Charts;

public partial class BatteryHealthChart
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromMinutes(30));
    private ApexChart<ChartDataEntry<string, decimal>> _apexChart = null!;
    private ApexChartOptions<ChartDataEntry<string, decimal>> _options { get; set; } = new();

    private ChartData2<string, decimal> BatteryHealth { get; set; } = new();

    public BatteryHealthChart()
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
                    Formatter = @"function (value) { return value + ' %'; }"
                }
            }
        };
        _options.Theme = new Theme
        {
            Mode = Mode.Dark,
            Palette = PaletteType.Palette1
        };
        _options.Colors = new List<string> { "#B6FED6", "#5DE799", "#59C7D4" };
        _options.Stroke = new Stroke
        {
            Curve = Curve.Smooth
        };
        _options.Fill = new Fill
        {
            Type = new List<FillType> { FillType.Solid, FillType.Solid, FillType.Solid },
            Opacity = new Opacity(1, 1, 1)
        };

        BatteryHealth.Description = "Thuisbatterij: gezondheid";
        BatteryHealth.Series1Description = "Geregistreerde gezondheid";
        BatteryHealth.Series2Description = "Berekende gezondheid";
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _ = RunTimer();
        }

        return base.OnAfterRenderAsync(firstRender);
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

            var response = await mediator.Send(new GetBatteryEnergyHistoryQuery
            {
                From = from,
                To = to,
                Unit = EnergyHistoryUnit.Month
            });

            BatteryHealth.Clear();
            BatteryHealth.Series1.AddRange(response.Entries.Select(x => new ChartDataEntry<string, decimal>
            {
                XValue = $"{x.Date:MMM yyyy}",
                YValue = Math.Min(100, Math.Round(x.StateOfHealth, 2) * 100)
            }));
            BatteryHealth.Series2.AddRange(response.Entries.Select(x => new ChartDataEntry<string, decimal>
            {
                XValue = $"{x.Date:MMM yyyy}",
                YValue = Math.Min(100, Math.Round(x.CalculatedStateOfHealth, 2) * 100)
            }));

            var minimumStateOfHealth = Math.Round(response.Entries.Min(x => x.StateOfHealth), 2) * 100;
            var minimumCalculatedStateOfHealth = Math.Round(response.Entries.Min(x => x.CalculatedStateOfHealth), 2) * 100;

            _options.Yaxis[0].Min = Math.Min(minimumStateOfHealth - 5, minimumCalculatedStateOfHealth - 5);
            _options.Yaxis[0].Max = 100;

            await _apexChart.UpdateSeriesAsync(true);
            await _apexChart.UpdateOptionsAsync(true, true, false);

            await InvokeAsync(StateHasChanged);
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

    private IEnumerable<ChartDataEntry<TX, TY>> FillData<TX, TY>(IEnumerable<ChartDataEntry<TX, TY>> source, int total, Func<int, TX> generator)
    {
        var result = new List<ChartDataEntry<TX, TY>>();

        result.AddRange(source);
        result.AddRange(Enumerable.Range(source.Count(), total - source.Count()).Select(n => new ChartDataEntry<TX, TY>
        {
            XValue = generator(n),
            YValue = default!
        }));

        return result;
    }
}