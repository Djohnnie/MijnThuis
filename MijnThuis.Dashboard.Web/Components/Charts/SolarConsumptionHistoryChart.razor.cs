using ApexCharts;
using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Solar;
using MijnThuis.Dashboard.Web.Model;
using MijnThuis.Dashboard.Web.Model.Charts;
using System.Globalization;

namespace MijnThuis.Dashboard.Web.Components.Charts;

public partial class SolarConsumptionHistoryChart
{
    [CascadingParameter]
    public NotifyingDarkMode DarkMode { get; set; }

    private enum HistoryType
    {
        PerDayInMonth,
        PerMonthInYear,
        PerYearInLifetime
    }

    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromMinutes(15));
    private ApexChart<ChartDataEntry<string, decimal>> _apexChart = null!;
    private ApexChartOptions<ChartDataEntry<string, decimal>> _options { get; set; } = new();

    private ChartData4<string, decimal> SolarPower { get; set; } = new();

    private HistoryType _historyType = HistoryType.PerMonthInYear;
    private DateTime _historyDate = new DateTime(DateTime.Today.Year, 1, 1);

    public string TitleDescription { get; set; }

    public SolarConsumptionHistoryChart()
    {
        _options.Chart = new Chart
        {
            Stacked = true,
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
                Labels = new YAxisLabels
                {
                    Formatter = @"function (value) { return value + ' kWh'; }"
                }
            }
        };
        _options.Theme = new Theme
        {
            Mode = Mode.Dark,
            Palette = PaletteType.Palette1
        };
        _options.Colors = new List<string> { "#B0D8FD", "#93B6FB", "#FBB550", "#C021C7" };
        _options.Stroke = new Stroke
        {
            Show = false
        };
        _options.Fill = new Fill
        {
            Type = new List<FillType> { FillType.Solid, FillType.Solid, FillType.Solid },
            Opacity = new Opacity(1, 1, 1)
        };

        SolarPower.Description = "Zonne-energie: Consumptie";
        SolarPower.Series1Description = "Consumptie vanuit batterij";
        SolarPower.Series2Description = "Consumptie vanuit PV";
        SolarPower.Series3Description = "Consumptie vanuit het net";
        SolarPower.Series4Description = "Batterij opladen vanuit het net";
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

            var response = await mediator.Send(new GetSolarEnergyHistoryQuery
            {
                From = _historyType switch
                {
                    HistoryType.PerDayInMonth => new DateTime(_historyDate.Year, _historyDate.Month, 1),
                    HistoryType.PerMonthInYear => new DateTime(_historyDate.Year, 1, 1),
                    HistoryType.PerYearInLifetime => new DateTime(2000, 1, 1),
                    _ => throw new InvalidOperationException()
                },
                To = _historyType switch
                {
                    HistoryType.PerDayInMonth => new DateTime(_historyDate.Year, _historyDate.Month, DateTime.DaysInMonth(_historyDate.Year, _historyDate.Month)),
                    HistoryType.PerMonthInYear => new DateTime(_historyDate.Year, 12, 31),
                    HistoryType.PerYearInLifetime => new DateTime(DateTime.Today.Year, 12, 31),
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

            var entries = response.Entries;

            SolarPower.Clear();
            switch (_historyType)
            {
                case HistoryType.PerDayInMonth:
                    TitleDescription = string.Create(CultureInfo.GetCultureInfo("nl-be"), $"De consumptie in {_historyDate:MMMM yyyy}");
                    var daysInMonth = DateTime.DaysInMonth(_historyDate.Year, _historyDate.Month);
                    SolarPower.Series1.AddRange(FillData(entries.Select(x => new ChartDataEntry<string, decimal>
                    {
                        XValue = $"{x.Date:dd/MM/yyyy}",
                        YValue = Math.Round(x.ConsumptionFromBattery)
                    }), daysInMonth, n => $"{_historyDate.AddDays(n):dd/MM/yyyy}"));
                    SolarPower.Series2.AddRange(FillData(entries.Select(x => new ChartDataEntry<string, decimal>
                    {
                        XValue = $"{x.Date:dd/MM/yyyy}",
                        YValue = Math.Round(x.ConsumptionFromSolar)
                    }), daysInMonth, n => $"{_historyDate.AddDays(n):dd/MM/yyyy}"));
                    SolarPower.Series3.AddRange(FillData(entries.Select(x => new ChartDataEntry<string, decimal>
                    {
                        XValue = $"{x.Date:dd/MM/yyyy}",
                        YValue = Math.Round(x.ConsumptionFromGrid)
                    }), daysInMonth, n => $"{_historyDate.AddDays(n):dd/MM/yyyy}"));
                    SolarPower.Series4.AddRange(FillData(entries.Select(x => new ChartDataEntry<string, decimal>
                    {
                        XValue = $"{x.Date:dd/MM/yyyy}",
                        YValue = -Math.Round(x.ImportToBattery)
                    }), daysInMonth, n => $"{_historyDate.AddDays(n):dd/MM/yyyy}"));
                    break;
                case HistoryType.PerMonthInYear:
                    TitleDescription = string.Create(CultureInfo.GetCultureInfo("nl-be"), $"De consumptie in {_historyDate:yyyy}");
                    SolarPower.Series1.AddRange(FillData(entries.Select(x => new ChartDataEntry<string, decimal>
                    {
                        XValue = $"{x.Date:MM/yyyy}",
                        YValue = Math.Round(x.ConsumptionFromBattery)
                    }), 12, n => $"{_historyDate.AddMonths(n):MM/yyyy}"));
                    SolarPower.Series2.AddRange(FillData(entries.Select(x => new ChartDataEntry<string, decimal>
                    {
                        XValue = $"{x.Date:MM/yyyy}",
                        YValue = Math.Round(x.ConsumptionFromSolar)
                    }), 12, n => $"{_historyDate.AddMonths(n):MM/yyyy}"));
                    SolarPower.Series3.AddRange(FillData(entries.Select(x => new ChartDataEntry<string, decimal>
                    {
                        XValue = $"{x.Date:MM/yyyy}",
                        YValue = Math.Round(x.ConsumptionFromGrid)
                    }), 12, n => $"{_historyDate.AddMonths(n):MM/yyyy}"));
                    SolarPower.Series4.AddRange(FillData(entries.Select(x => new ChartDataEntry<string, decimal>
                    {
                        XValue = $"{x.Date:MM/yyyy}",
                        YValue = -Math.Round(x.ImportToBattery)
                    }), 12, n => $"{_historyDate.AddMonths(n):MM/yyyy}"));
                    break;
                case HistoryType.PerYearInLifetime:
                    TitleDescription = string.Create(CultureInfo.GetCultureInfo("nl-be"), $"De consumptie gedurende de totale levensduur");
                    SolarPower.Series1.AddRange(entries.Select(x => new ChartDataEntry<string, decimal>
                    {
                        XValue = $"{x.Date:yyyy}",
                        YValue = Math.Round(x.ConsumptionFromBattery)
                    }).OrderBy(x => x.XValue));
                    SolarPower.Series2.AddRange(entries.Select(x => new ChartDataEntry<string, decimal>
                    {
                        XValue = $"{x.Date:yyyy}",
                        YValue = Math.Round(x.ConsumptionFromSolar)
                    }).OrderBy(x => x.XValue));
                    SolarPower.Series3.AddRange(entries.Select(x => new ChartDataEntry<string, decimal>
                    {
                        XValue = $"{x.Date:yyyy}",
                        YValue = Math.Round(x.ConsumptionFromGrid)
                    }).OrderBy(x => x.XValue));
                    SolarPower.Series4.AddRange(entries.Select(x => new ChartDataEntry<string, decimal>
                    {
                        XValue = $"{x.Date:yyyy}",
                        YValue = -Math.Round(x.ImportToBattery)
                    }).OrderBy(x => x.XValue));
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
        _historyDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        await RefreshData();
    }

    private async Task HistoryPerMonthInYearCommand()
    {
        _historyType = HistoryType.PerMonthInYear;
        _historyDate = new DateTime(DateTime.Today.Year, 1, 1);
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

    private IEnumerable<ChartDataEntry<TX, TY>> FillData<TX, TY>(IEnumerable<ChartDataEntry<TX, TY>> source, int total, Func<int, TX> generator)
    {
        var result = new List<ChartDataEntry<TX, TY>>();

        result.AddRange(source);
        result.AddRange(Enumerable.Range(source.Count(), total - source.Count()).Select(n => new ChartDataEntry<TX, TY>
        {
            XValue = generator(n),
            YValue = default!
        }));

        return result.OrderBy(x => x.XValue);
    }
}