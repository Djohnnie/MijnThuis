using ApexCharts;
using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Application.Power.Queries;
using MijnThuis.Dashboard.Web.Model;
using MijnThuis.Dashboard.Web.Model.Charts;
using System.Globalization;

namespace MijnThuis.Dashboard.Web.Components.Charts;

public partial class DayAheadEnergyCostChart
{
    [CascadingParameter]
    public NotifyingDarkMode DarkMode { get; set; }

    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromMinutes(5));
    private ApexChart<ChartDataEntry<string, decimal?>> _apexChart = null!;
    private ApexChartOptions<ChartDataEntry<string, decimal?>> _options { get; set; } = new();

    private ChartData6<string, decimal?> DayAheadEnergyPrices { get; set; } = new();
    private DateTime _selectedDate = DateTime.Today;

    public string TitleDescription { get; set; }

    public DayAheadEnergyCostChart()
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
                    }
                },
            }
        };
        _options.Xaxis = new XAxis
        {
            Type = XAxisType.Category,
            OverwriteCategories = Enumerable.Range(0, 24 * 4).Select(x => new DateTime().AddMinutes(15 * x).Minute == 0 ? $"{new DateTime().AddMinutes(15 * x):HH:mm}" : "").ToList(),
        };
        _options.Yaxis = new List<YAxis>
        {
            new YAxis
            {
                DecimalsInFloat = 3,
                Labels = new YAxisLabels
                {
                    Formatter = @"function (value) { return value === null ? 'geen waarde' : value + ' kWh'; }"
                },
                Show = false
            },
            new YAxis
            {
                DecimalsInFloat = 3,
                Labels = new YAxisLabels
                {
                    Formatter = @"function (value) { return value === null ? 'geen waarde' : value + ' €'; }"
                },
                Show = false
            },
            new YAxis
            {
                DecimalsInFloat = 0,
                Labels = new YAxisLabels
                {
                    Formatter = @"function (value) { return value + ' €c/kWh'; }"
                },
                Show = true
            },
            new YAxis
            {
                DecimalsInFloat = 0,
                Labels = new YAxisLabels
                {
                    Formatter = @"function (value) { return value == 0 ? ' Ja' : ' Neen'; }"
                },
                Show = false
            },
            new YAxis
            {
                DecimalsInFloat = 0,
                Min = 0,
                Max = 100,
                Labels = new YAxisLabels
                {
                    Formatter = @"function (value) { return value === null ? 'geen waarde' : value + ' %'; }"
                },
                Show = false
            },
            new YAxis
            {
                DecimalsInFloat = 0,
                Min = 0,
                Max = 100,
                Labels = new YAxisLabels
                {
                    Formatter = @"function (value) { return value === null ? 'geen waarde' : value + ' %'; }"
                },
                Show = false
            }
        };
        _options.Theme = new Theme
        {
            Mode = Mode.Dark,
            Palette = PaletteType.Palette1
        };
        _options.Colors = new List<string> { "#5DE799", "#FF9090", "#B0D8FD", "#FF0000", "#000000", "#DDDDDD" };
        _options.Stroke = new Stroke
        {
            Curve = Curve.Smooth,
            Width = new Size(4, 4, 4, 8, 2, 2),
            LineCap = LineCap.Round,
            DashArray = [0, 0, 0, 0, 0, 0]
        };
        _options.Fill = new Fill
        {
            Type = new List<FillType> { FillType.Solid, FillType.Solid, FillType.Solid, FillType.Solid, FillType.Solid, FillType.Solid },
            Opacity = new Opacity(1, 1, 1, 1, 1, 1)
        };

        DayAheadEnergyPrices.Description = "Elektriciteit: Dynamische tarieven";
        DayAheadEnergyPrices.Series1Description = "Verbruik in kWh";
        DayAheadEnergyPrices.Series2Description = "Kost in €";
        DayAheadEnergyPrices.Series3Description = "Dynamisch tarief in €c/kWh";
        DayAheadEnergyPrices.Series4Description = "Batterij opladen via netstroom";
        DayAheadEnergyPrices.Series5Description = "Batterijpercentage";
        DayAheadEnergyPrices.Series6Description = "Batterijpercentage";
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

            var minutes = DateTime.Now.Minute / 15 * 15;
            var now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, minutes, 0);

            var response = await mediator.Send(new GetDayAheadEnergyCostQuery
            {
                Date = _selectedDate
            });

            var entries = response.Entries;

            DayAheadEnergyPrices.Clear();
            DayAheadEnergyPrices.Series1.AddRange(entries.Select(x => new ChartDataEntry<string, decimal?>
            {
                XValue = $"{x.Date:HH:mm}",
                YValue = x.Consumption
            }));
            DayAheadEnergyPrices.Series2.AddRange(entries.Select(x => new ChartDataEntry<string, decimal?>
            {
                XValue = $"{x.Date:HH:mm}",
                YValue = x.ConsumptionCost
            }));
            DayAheadEnergyPrices.Series3.AddRange(entries.Select(x => new ChartDataEntry<string, decimal?>
            {
                XValue = $"{x.Date:HH:mm}",
                YValue = x.ConsumptionPrice
            }));
            DayAheadEnergyPrices.Series4.AddRange(entries.Select(x => new ChartDataEntry<string, decimal?>
            {
                XValue = $"{x.Date:HH:mm}",
                YValue = x.ConsumptionPriceShouldCharge
            }));
            DayAheadEnergyPrices.Series5.AddRange(entries.Select(x => new ChartDataEntry<string, decimal?>
            {
                XValue = $"{x.Date:HH:mm}",
                YValue = x.BatteryLevel
            }));
            DayAheadEnergyPrices.Series6.AddRange(entries.Select(x => new ChartDataEntry<string, decimal?>
            {
                XValue = $"{x.Date:HH:mm}",
                YValue = x.EstimatedBatteryLevel
            }));

            TitleDescription = string.Create(CultureInfo.GetCultureInfo("nl-be"), $"Dynamische tarieven voor {_selectedDate:D}");

            await InvokeAsync(StateHasChanged);
            await Task.Delay(100);
            await _apexChart.UpdateSeriesAsync(true);
            await _apexChart.UpdateOptionsAsync(true, true, true);
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