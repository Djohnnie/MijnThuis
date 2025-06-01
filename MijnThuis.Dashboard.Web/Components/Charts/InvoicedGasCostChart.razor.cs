using ApexCharts;
using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Heating;
using MijnThuis.Dashboard.Web.Model;
using MijnThuis.Dashboard.Web.Model.Charts;

namespace MijnThuis.Dashboard.Web.Components.Charts;

public partial class InvoicedGasCostChart
{
    [CascadingParameter]
    public NotifyingDarkMode DarkMode { get; set; }

    private ApexChart<ChartDataEntry<string, decimal>> _apexChart = null!;
    private ApexChartOptions<ChartDataEntry<string, decimal>> _options { get; set; } = new();
    private ChartData2<string, decimal> Chart { get; set; } = new();
    private DateTime _selectedYear = new(DateTime.Today.Year, 1, 1);

    public InvoicedGasCostChart()
    {
        _options.Chart = new Chart
        {
            Toolbar = new Toolbar { Show = false },
            Zoom = new Zoom { Enabled = false },
            Background = "#373740",
        };
        _options.Yaxis = new List<YAxis>
        {
            new YAxis
            {
                DecimalsInFloat = 1,
                Labels = new YAxisLabels
                {
                    Formatter = @"function (value) { return Math.round(value * 100) / 100 + ' €'; }"
                }
            }
        };
        _options.Theme = new Theme { Mode = Mode.Dark, Palette = PaletteType.Palette1 };
        _options.Colors = new List<string> { "#95B6F8", "#6FE59D" };
        _options.Stroke = new Stroke { Show = false };
        _options.Fill = new Fill { Type = new List<FillType> { FillType.Solid, FillType.Solid }, Opacity = new Opacity(1, 1) };
        Chart.Series1Description = "Vorig jaar";
        Chart.Series2Description = "Dit jaar";
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
            await RefreshData();
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task RefreshData()
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var response = await mediator.Send(new GetInvoicedGasCostQuery
            {
                Year = _selectedYear == DateTime.MinValue ? 0 : _selectedYear.Year
            });

            Chart.Clear();

            if (_selectedYear == DateTime.MinValue)
            {
                Chart.Series1.AddRange(response.AllYears.Select(x => new ChartDataEntry<string, decimal>
                {
                    XValue = x.Date.ToString("yyyy"),
                    YValue = x.GasAmount
                }));

                Chart.Description = "Facturen gas voor de hele levensduur";
                Chart.Series1Description = "Facturen per jaar";
                Chart.Series2Description = " ";
            }
            else
            {
                Chart.Series1.AddRange(response.LastYear.Select(x => new ChartDataEntry<string, decimal>
                {
                    XValue = x.Date.ToString("MMMM"),
                    YValue = x.GasAmount
                }));
                Chart.Series2.AddRange(response.ThisYear.Select(x => new ChartDataEntry<string, decimal>
                {
                    XValue = x.Date.ToString("MMMM"),
                    YValue = x.GasAmount
                }));

                Chart.Description = $"Facturen gas voor geselecteerde jaar {_selectedYear.Year}";
                Chart.Series1Description = $"Facturen in {_selectedYear.Year - 1}";
                Chart.Series2Description = $"Facturen in {_selectedYear.Year}";
                _options.Series[1].Hidden = false;
            }

            await InvokeAsync(StateHasChanged);
            await Task.Delay(100);
            await _apexChart.UpdateSeriesAsync(true);
            await _apexChart.UpdateOptionsAsync(true, true, false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to refresh invoiced gas cost chart data");
        }
    }

    private async Task NavigateBeforeCommand()
    {
        if (_selectedYear != DateTime.MinValue)
        {
            _selectedYear = _selectedYear.AddYears(-1);
            await RefreshData();
        }
    }

    private async Task InYearCommand()
    {
        _selectedYear = new DateTime(DateTime.Today.Year, 1, 1);

        await RefreshData();
    }

    private async Task InLifetimeCommand()
    {
        _selectedYear = DateTime.MinValue;

        await RefreshData();
    }

    private async Task NavigateNextCommand()
    {
        if (_selectedYear != DateTime.MinValue)
        {
            _selectedYear = _selectedYear.AddYears(1);
            await RefreshData();
        }
    }

    public void Dispose() { }
}