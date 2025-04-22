using ApexCharts;
using MediatR;
using MijnThuis.Contracts.Solar;
using MijnThuis.Dashboard.Web.Model.Charts;

namespace MijnThuis.Dashboard.Web.Components.Charts;

public partial class SolarSelfConsumptionChart
{
    private enum HistoryType
    {
        PerDay,
        PerMonth,
        PerYear
    }

    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromHours(1));
    private ApexChart<ChartDataEntry<string, decimal>> _apexChart = null!;
    private ApexChartOptions<ChartDataEntry<string, decimal>> _options { get; set; } = new();

    private ChartData2<string, decimal> SolarPower { get; set; } = new();

    private HistoryType _historyType = HistoryType.PerMonth;
    private DateTime _historyDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    public SolarSelfConsumptionChart()
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
        _options.Colors = new List<string> { "#95B6F8", "#6FE59D" };
        _options.Stroke = new Stroke
        {
            Show = false
        };
        _options.Fill = new Fill
        {
            Type = new List<FillType> { FillType.Solid, FillType.Solid, FillType.Solid },
            Opacity = new Opacity(1, 1, 1)
        };

        SolarPower.Description = $"Zonne-energie: Zelfconsumptie en zelfsufficiëntie";
        SolarPower.Series1Description = "Zelfconsumptie";
        SolarPower.Series2Description = "Zelfsufficiëntie";
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

            var response = await mediator.Send(new GetSolarSelfConsumptionQuery
            {
                Date = _historyType switch
                {
                    HistoryType.PerDay => _historyDate,
                    HistoryType.PerMonth => new DateTime(_historyDate.Year, 1, 1),
                    HistoryType.PerYear => new DateTime(_historyDate.Year, 1, 1),
                    _ => throw new InvalidOperationException()
                },
                Range = _historyType switch
                {
                    HistoryType.PerDay => SolarSelfConsumptionRange.Day,
                    HistoryType.PerMonth => SolarSelfConsumptionRange.Month,
                    HistoryType.PerYear => SolarSelfConsumptionRange.Year,
                    _ => throw new InvalidOperationException()
                }
            });

            SolarPower.Clear();
            switch (_historyType)
            {
                case HistoryType.PerDay:
                    SolarPower.Series1.AddRange(response.Entries
                        .Select(x => new ChartDataEntry<string, decimal>
                        {
                            XValue = $"{x.Date:dd MMM}",
                            YValue = x.SelfConsumption
                        }));
                    SolarPower.Series2.AddRange(response.Entries
                        .Select(x => new ChartDataEntry<string, decimal>
                        {
                            XValue = $"{x.Date:dd MMM}",
                            YValue = x.SelfSufficiency
                        }));
                    break;
                case HistoryType.PerMonth:
                    SolarPower.Series1.AddRange(response.Entries
                        .Select(x => new ChartDataEntry<string, decimal>
                        {
                            XValue = $"{x.Date:MMM yyyy}",
                            YValue = x.SelfConsumption
                        }));
                    SolarPower.Series2.AddRange(response.Entries
                        .Select(x => new ChartDataEntry<string, decimal>
                        {
                            XValue = $"{x.Date:MMM yyyy}",
                            YValue = x.SelfSufficiency
                        }));
                    break;
                case HistoryType.PerYear:
                    SolarPower.Series1.AddRange(response.Entries
                        .Select(x => new ChartDataEntry<string, decimal>
                        {
                            XValue = $"{x.Date:yyyy}",
                            YValue = x.SelfConsumption
                        }));
                    SolarPower.Series2.AddRange(response.Entries
                        .Select(x => new ChartDataEntry<string, decimal>
                        {
                            XValue = $"{x.Date:yyyy}",
                            YValue = x.SelfSufficiency
                        }));
                    break;
            }

            await _apexChart.UpdateSeriesAsync(true);

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh graph data");
        }
    }

    private async Task NavigateBeforeCommand()
    {
        _historyDate = _historyType switch
        {
            HistoryType.PerDay => _historyDate.AddMonths(-1),
            HistoryType.PerMonth => _historyDate.AddYears(-1),
            HistoryType.PerYear => _historyDate,
            _ => throw new InvalidOperationException()
        };

        await RefreshData();
    }

    private async Task HistoryPerDayInMonthCommand()
    {
        _historyType = HistoryType.PerDay;
        await RefreshData();
    }

    private async Task HistoryPerMonthInYearCommand()
    {
        _historyType = HistoryType.PerMonth;
        await RefreshData();
    }

    private async Task HistoryPerYearInLifetimeCommand()
    {
        _historyType = HistoryType.PerYear;
        await RefreshData();
    }

    private async Task NavigateNextCommand()
    {
        _historyDate = _historyType switch
        {
            HistoryType.PerDay => _historyDate.AddMonths(1),
            HistoryType.PerMonth => _historyDate.AddYears(1),
            HistoryType.PerYear => _historyDate,
            _ => throw new InvalidOperationException()
        };

        await RefreshData();
    }

    public void Dispose()
    {
        _periodicTimer.Dispose();
    }
}