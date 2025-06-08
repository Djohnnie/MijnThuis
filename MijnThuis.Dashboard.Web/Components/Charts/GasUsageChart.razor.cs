using ApexCharts;
using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Heating;
using MijnThuis.Dashboard.Web.Model;
using MijnThuis.Dashboard.Web.Model.Charts;

namespace MijnThuis.Dashboard.Web.Components.Charts;

public partial class GasUsageChart
{
    [CascadingParameter]
    public NotifyingDarkMode DarkMode { get; set; }

    private enum HistoryType
    {
        PerDayInMonth,
        PerMonthInYear,
        PerYearInLifetime
    }

    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromHours(1));
    private ApexChart<ChartDataEntry<string, decimal>> _apexChart = null!;
    private ApexChartOptions<ChartDataEntry<string, decimal>> _options { get; set; } = new();
    private ChartData1<string, decimal> GasUsage { get; set; } = new();

    private HistoryType _historyType = HistoryType.PerMonthInYear;
    private DateTime _selectedDate = new DateTime(DateTime.Today.Year, 1, 1);

    public GasUsageChart()
    {
        _options.Chart = new Chart
        {
            Toolbar = new Toolbar { Show = false },
            Zoom = new Zoom { Enabled = false },
            Background = "#373740",
        };
        _options.Responsive = new List<Responsive<ChartDataEntry<string, decimal>>>
        {
            new Responsive<ChartDataEntry<string, decimal>>
            {
                Breakpoint = 700,
                Options = new ApexChartOptions<ChartDataEntry<string, decimal>>
                {
                    Legend = new Legend { Show = false },
                    Xaxis = new XAxis { Labels = new XAxisLabels { Show = false } },
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
                    Formatter = "function (value) { return Math.round(value * 10) / 10 + ' m³'; }"
                }
            }
        };
        _options.Theme = new Theme { Mode = Mode.Dark, Palette = PaletteType.Palette1 };
        _options.Colors = new List<string> { "#FBB550" };
        _options.Stroke = new Stroke { Show = false };
        _options.Fill = new Fill { Type = new List<FillType> { FillType.Solid }, Opacity = new Opacity(1) };
        GasUsage.Series1Description = "Totaal gasverbruik";
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
            try { await RefreshData(); }
            catch (ObjectDisposedException) { _periodicTimer.Dispose(); break; }
        }
    }

    private async Task RefreshData()
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var (unit, dateFormat, description) = _historyType switch
            {
                HistoryType.PerDayInMonth => (GasUsageUnit.Day, "dd MMMM", $"per dag in {_selectedDate:MMMM yyyy}"),
                HistoryType.PerMonthInYear => (GasUsageUnit.Month, "MMMM yyyy", $"per maand in {_selectedDate:yyyy}"),
                HistoryType.PerYearInLifetime => (GasUsageUnit.Year, "yyyy", "per jaar"),
                _ => throw new InvalidOperationException()
            };

            var response = await mediator.Send(new GetGasUsageQuery { Date = _selectedDate, Unit = unit });
            var entries = response.Entries.OrderBy(x => x.Date);

            GasUsage.Series1 = entries.Select(x => new ChartDataEntry<string, decimal>
            {
                XValue = x.Date.ToString(dateFormat),
                YValue = Math.Round(x.GasAmount, 2)
            }).ToList();

            GasUsage.Series1Description = $"Totaal gasverbruik {description}";

            await InvokeAsync(StateHasChanged);
            await Task.Delay(100);
            await _apexChart.UpdateSeriesAsync(true);
            await _apexChart.UpdateOptionsAsync(true, false, false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh gas usage graph data");
        }
    }

    private async Task NavigateBeforeCommand()
    {
        _selectedDate = _historyType switch
        {
            HistoryType.PerDayInMonth => _selectedDate.AddMonths(-1),
            HistoryType.PerMonthInYear => _selectedDate.AddYears(-1),
            HistoryType.PerYearInLifetime => _selectedDate,
            _ => throw new InvalidOperationException()
        };
        await RefreshData();
    }

    private async Task HistoryPerDayInMonthCommand()
    {
        _selectedDate = DateTime.Today;
        _historyType = HistoryType.PerDayInMonth;
        await RefreshData();
    }

    private async Task HistoryPerMonthInYearCommand()
    {
        _selectedDate = DateTime.Today;
        _historyType = HistoryType.PerMonthInYear;
        await RefreshData();
    }

    private async Task HistoryPerYearInLifetimeCommand()
    {
        _selectedDate = DateTime.Today;
        _historyType = HistoryType.PerYearInLifetime;
        await RefreshData();
    }

    private async Task NavigateNextCommand()
    {
        _selectedDate = _historyType switch
        {
            HistoryType.PerDayInMonth => _selectedDate.AddMonths(1),
            HistoryType.PerMonthInYear => _selectedDate.AddYears(1),
            HistoryType.PerYearInLifetime => _selectedDate,
            _ => throw new InvalidOperationException()
        };
        await RefreshData();
    }

    public void Dispose() => _periodicTimer.Dispose();
}