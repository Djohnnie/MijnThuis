using ApexCharts;
using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Car;
using MijnThuis.Contracts.Solar;
using MijnThuis.Dashboard.Web.Model;
using MijnThuis.Dashboard.Web.Model.Charts;
using System.Globalization;

namespace MijnThuis.Dashboard.Web.Components.Charts;

public partial class CarChargingHistoryChart
{
    [CascadingParameter]
    public NotifyingDarkMode DarkMode { get; set; }

    private enum HistoryType
    {
        PerMonth,
        PerYear
    }

    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromMinutes(30));
    private ApexChart<ChartDataEntry<string, decimal>> _apexChart = null!;
    private ApexChartOptions<ChartDataEntry<string, decimal>> _options { get; set; } = new();

    private ChartData1<string, decimal> ChargingHistory { get; set; } = new();

    private HistoryType _historyType = HistoryType.PerMonth;
    private DateTime _historyDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    private string TitleDescription { get; set; }

    public CarChargingHistoryChart()
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
            Background = "#373740"
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
            OverwriteCategories = ["Jan", "Feb", "Maa", "Apr", "Mei", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dec"],
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
        _options.Colors = new List<string> { "#5DE799" };
        _options.Stroke = new Stroke
        {
            Show = false
        };
        _options.Fill = new Fill
        {
            Type = new List<FillType> { FillType.Solid, FillType.Solid, FillType.Solid },
            Opacity = new Opacity(1, 1, 1)
        };

        ChargingHistory.Description = $"Auto opladen uit zonne-energie";
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

            var response = await mediator.Send(new GetCarChargingHistoryQuery
            {
                From = _historyType switch
                {
                    HistoryType.PerMonth => _historyDate,
                    HistoryType.PerYear => new DateTime(_historyDate.Year, 1, 1),
                    _ => throw new InvalidOperationException()
                },
                To = _historyType switch
                {
                    HistoryType.PerMonth => _historyDate.AddMonths(1).AddDays(-1),
                    HistoryType.PerYear => _historyDate.AddYears(1).AddDays(-1),
                    _ => throw new InvalidOperationException()
                },
                Unit = _historyType switch
                {
                    HistoryType.PerMonth => EnergyHistoryUnit.Day,
                    HistoryType.PerYear => EnergyHistoryUnit.Month,
                    _ => throw new InvalidOperationException()
                }
            });

            ChargingHistory.Clear();

            switch (_historyType)
            {
                case HistoryType.PerMonth:
                    _options.Xaxis.OverwriteCategories = Enumerable.Range(1, DateTime.DaysInMonth(_historyDate.Year, _historyDate.Month)).Select(x => $"{x}").ToList();

                    ChargingHistory.Series1Description = TitleDescription =
                        string.Create(CultureInfo.GetCultureInfo("nl-be"), $"Oplaadsessies in de maand {_historyDate:MMMM yyyy}");

                    ChargingHistory.Series1.AddRange(FillData(response.Entries
                        .Select(x => new ChartDataEntry<string, decimal>
                        {
                            XValue = $"{x.Date:dd MMMM yyyy}",
                            YValue = Math.Round(x.EnergyCharged / 1000, 2)
                        }), DateTime.DaysInMonth(_historyDate.Year, _historyDate.Month), n => $"{new DateTime(_historyDate.Year, _historyDate.Month, n):dd MMMM yyyy}"));
                    break;
                case HistoryType.PerYear:
                    _options.Xaxis.OverwriteCategories = ["Jan", "Feb", "Maa", "Apr", "Mei", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dec"];

                    ChargingHistory.Series1Description = TitleDescription =
                        $"Oplaadsessies in het jaar {_historyDate:yyyy}";

                    ChargingHistory.Series1.AddRange(FillData(response.Entries
                        .Select(x => new ChartDataEntry<string, decimal>
                        {
                            XValue = $"{x.Date:MMMM yyyy}",
                            YValue = Math.Round(x.EnergyCharged / 1000, 2)
                        }), 12, n => $"{new DateTime(_historyDate.Year, n, 1):MMMM yyyy}"));
                    break;
            }

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

    private async Task NavigateBeforeCommand()
    {
        _historyDate = _historyType switch
        {
            HistoryType.PerMonth => _historyDate.AddMonths(-1),
            HistoryType.PerYear => _historyDate.AddYears(-1),
            _ => throw new InvalidOperationException()
        };

        await RefreshData();
    }

    private async Task HistoryPerMonthCommand()
    {
        _historyType = HistoryType.PerMonth;
        await RefreshData();
    }

    private async Task HistoryPerYearCommand()
    {
        _historyType = HistoryType.PerYear;
        await RefreshData();
    }

    private async Task NavigateNextCommand()
    {
        _historyDate = _historyType switch
        {
            HistoryType.PerMonth => _historyDate.AddMonths(1),
            HistoryType.PerYear => _historyDate.AddYears(1),
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

        for (int i = 1; i <= total; i++)
        {
            var fromSource = source.SingleOrDefault(x => x.XValue.Equals(generator(i)));
            if (fromSource != null)
            {
                result.Add(fromSource);
                continue;
            }
            else
            {
                result.Add(new ChartDataEntry<TX, TY>
                {
                    XValue = generator(i),
                    YValue = default!
                });
            }
        }

        return result;
    }
}