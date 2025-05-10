using ApexCharts;
using MediatR;
using MijnThuis.Application.Power.Queries;
using MijnThuis.Contracts.Power;
using MijnThuis.Contracts.Solar;
using MijnThuis.Dashboard.Web.Model.Charts;

namespace MijnThuis.Dashboard.Web.Components.Charts;

public partial class EnergyCostHistoryChart
{
    private enum HistoryType
    {
        PerDayInMonth,
        PerMonthInYear,
        PerYearInLifetime
    }

    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromHours(1));
    private ApexChart<ChartDataEntry<string, decimal>> _apexChart = null!;
    private ApexChartOptions<ChartDataEntry<string, decimal>> _options { get; set; } = new();

    private ChartData2<string, decimal> SolarPower { get; set; } = new();

    private HistoryType _historyType = HistoryType.PerMonthInYear;
    private DateTime _historyDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    public EnergyCostHistoryChart()
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
                DecimalsInFloat = 1,
                Labels = new YAxisLabels
                {
                    Formatter = @"function (value) { return Math.round(value * 10) / 10 + ' €'; }"
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
            Type = new List<FillType> { FillType.Solid, FillType.Solid },
            Opacity = new Opacity(1, 1)
        };

        SolarPower.Description = $"Elektriciteitskost: Consumptie en injectie";
        SolarPower.Series1Description = "Kost voor consumptie";
        SolarPower.Series2Description = "Kost voor injectie";
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

            var response = await mediator.Send(new GetEnergyCostHistoryQuery
            {
                From = _historyType switch
                {
                    HistoryType.PerDayInMonth => _historyDate,
                    HistoryType.PerMonthInYear => new DateTime(_historyDate.Year, 1, 1),
                    HistoryType.PerYearInLifetime => new DateTime(2020, 1, 1),
                    _ => throw new InvalidOperationException()
                },
                To = _historyType switch
                {
                    HistoryType.PerDayInMonth => _historyDate.AddDays(DateTime.DaysInMonth(_historyDate.Year, _historyDate.Month) - 1),
                    HistoryType.PerMonthInYear => new DateTime(_historyDate.Year, 12, 31),
                    HistoryType.PerYearInLifetime => new DateTime(_historyDate.Year, 12, 31),
                    _ => throw new InvalidOperationException()
                },
                Unit = _historyType switch
                {
                    HistoryType.PerDayInMonth => EnergyHistoryUnit.Day,
                    HistoryType.PerMonthInYear => EnergyHistoryUnit.Month,
                    HistoryType.PerYearInLifetime => EnergyHistoryUnit.Year,
                    _ => throw new InvalidOperationException()
                }
            });

            var entries = response.Entries.OrderBy(x => x.Date);

            SolarPower.Clear();
            switch (_historyType)
            {
                case HistoryType.PerDayInMonth:
                    SolarPower.Series1.AddRange(entries
                        .Select(x => new ChartDataEntry<string, decimal>
                        {
                            XValue = $"{x.Date:dd MMM}",
                            YValue = Math.Round(x.ImportCost, 2)
                        }));
                    SolarPower.Series2.AddRange(entries
                        .Select(x => new ChartDataEntry<string, decimal>
                        {
                            XValue = $"{x.Date:dd MMM}",
                            YValue = Math.Round(x.ExportCost, 2)
                        }));
                    break;
                case HistoryType.PerMonthInYear:
                    SolarPower.Series1.AddRange(entries
                        .Select(x => new ChartDataEntry<string, decimal>
                        {
                            XValue = $"{x.Date:MMM yyyy}",
                            YValue = Math.Round(x.ImportCost, 2)
                        }));
                    SolarPower.Series2.AddRange(entries
                        .Select(x => new ChartDataEntry<string, decimal>
                        {
                            XValue = $"{x.Date:MMM yyyy}",
                            YValue = Math.Round(x.ExportCost, 2)
                        }));
                    break;
                case HistoryType.PerYearInLifetime:
                    SolarPower.Series1.AddRange(entries
                        .Select(x => new ChartDataEntry<string, decimal>
                        {
                            XValue = $"{x.Date:yyyy}",
                            YValue = Math.Round(x.ImportCost, 2)
                        }));
                    SolarPower.Series2.AddRange(entries
                        .Select(x => new ChartDataEntry<string, decimal>
                        {
                            XValue = $"{x.Date:yyyy}",
                            YValue = Math.Round(x.ExportCost, 2)
                        }));
                    break;
            }

            await InvokeAsync(StateHasChanged);
            await Task.Delay(100);
            await _apexChart.UpdateSeriesAsync(true);
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
            HistoryType.PerDayInMonth => _historyDate.AddMonths(-1),
            HistoryType.PerMonthInYear => _historyDate.AddYears(-1),
            HistoryType.PerYearInLifetime => _historyDate,
            _ => throw new InvalidOperationException()
        };

        await RefreshData();
    }

    private async Task HistoryPerDayInMonthCommand()
    {
        _historyType = HistoryType.PerDayInMonth;
        await RefreshData();
    }

    private async Task HistoryPerMonthInYearCommand()
    {
        _historyType = HistoryType.PerMonthInYear;
        await RefreshData();
    }

    private async Task HistoryPerYearInLifetimeCommand()
    {
        _historyType = HistoryType.PerYearInLifetime;
        await RefreshData();
    }

    private async Task NavigateNextCommand()
    {
        _historyDate = _historyType switch
        {
            HistoryType.PerDayInMonth => _historyDate.AddMonths(1),
            HistoryType.PerMonthInYear => _historyDate.AddYears(1),
            HistoryType.PerYearInLifetime => _historyDate,
            _ => throw new InvalidOperationException()
        };

        await RefreshData();
    }

    public void Dispose()
    {
        _periodicTimer.Dispose();
    }
}