using ApexCharts;
using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Power;
using MijnThuis.Dashboard.Web.Model;
using MijnThuis.Dashboard.Web.Model.Charts;
using System.Globalization;

namespace MijnThuis.Dashboard.Web.Components.Charts;

public partial class DayAheadEnergyPriceChart
{
    [CascadingParameter]
    public NotifyingDarkMode DarkMode { get; set; }

    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromHours(1));
    private ApexChart<ChartDataEntry<string, decimal>> _apexChart = null!;
    private ApexChartOptions<ChartDataEntry<string, decimal>> _options { get; set; } = new();

    private ChartData4<string, decimal> DayAheadEnergyPrices { get; set; } = new();
    private DateTime _selectedDate = DateTime.Today;

    public string TitleDescription { get; set; }

    public DayAheadEnergyPriceChart()
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
        _options.Annotations = new Annotations
        {
            Yaxis = new List<AnnotationsYAxis>
            {
                new AnnotationsYAxis
                {
                    Y = 0M,
                    BorderColor = "#FF8888",
                    StrokeDashArray = 0,
                    BorderWidth = 2
                }
            }
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
                    }
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
                    Formatter = @"function (value) { return value + ' €c/kWh'; }"
                }
            }
        };
        _options.Theme = new Theme
        {
            Mode = Mode.Dark,
            Palette = PaletteType.Palette1
        };
        _options.Colors = new List<string> { "#5DE799", "#5DE799", "#FBB550", "#B0D8FD" };
        _options.Stroke = new Stroke
        {
            Curve = Curve.Smooth,
            Width = new Size(3, 5, 3, 2),
            DashArray = [0, 0, 0, 3]
        };
        _options.Fill = new Fill
        {
            Type = new List<FillType> { FillType.Solid, FillType.Solid, FillType.Solid, FillType.Solid },
            Opacity = new Opacity(1, 1, 1, 0.5)
        };

        DayAheadEnergyPrices.Description = "Elektriciteit: Dynamische tarieven";
        DayAheadEnergyPrices.Series1Description = "Mega tarieven voor consumptie";
        DayAheadEnergyPrices.Series2Description = "Totale tarieven voor consumptie";
        DayAheadEnergyPrices.Series3Description = "Mega tarieven voor injectie";
        DayAheadEnergyPrices.Series4Description = "Dynamische tarieven";
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

            var now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);

            var response = await mediator.Send(new GetDayAheadEnergyPricesQuery
            {
                Date = _selectedDate
            });

            var entries = response.Entries;

            DayAheadEnergyPrices.Clear();
            DayAheadEnergyPrices.Series1.AddRange(entries.Select(x => new ChartDataEntry<string, decimal>
            {
                XValue = $"{x.Date:HH:mm}",
                YValue = x.ConsumptionPrice
            }));
            DayAheadEnergyPrices.Series2.AddRange(entries.Select(x => new ChartDataEntry<string, decimal>
            {
                XValue = $"{x.Date:HH:mm}",
                YValue = x.RealConsumptionPrice
            }));
            DayAheadEnergyPrices.Series3.AddRange(entries.Select(x => new ChartDataEntry<string, decimal>
            {
                XValue = $"{x.Date:HH:mm}",
                YValue = x.InjectionPrice
            }));
            DayAheadEnergyPrices.Series4.AddRange(entries.Select(x => new ChartDataEntry<string, decimal>
            {
                XValue = $"{x.Date:HH:mm}",
                YValue = x.Price
            }));

            TitleDescription = string.Create(CultureInfo.GetCultureInfo("nl-be"), $"Dynamische tarieven voor {_selectedDate:D}");

            _options.Annotations = _selectedDate.Date != DateTime.Today ? new Annotations
            {
                Xaxis = new List<AnnotationsXAxis>(),
                Points = new List<AnnotationsPoint>()
            } : new Annotations
            {
                Xaxis = new List<AnnotationsXAxis>
                {
                    new AnnotationsXAxis
                    {
                        X = $"{now:HH:mm}",
                        BorderColor = DarkMode.IsDarkMode ? "#FF8888" :"#FF0000",
                        StrokeDashArray = 2
                    }
                },
                Points = new List<AnnotationsPoint>
                {
                    new AnnotationsPoint
                    {
                        X = $"{now:HH:mm}",
                        Y = (double)(entries.SingleOrDefault(x=>x.Date == now)?.ConsumptionPrice ?? 0),
                        SeriesIndex = 0,
                        Marker = new AnnotationMarker
                        {
                            FillColor = "#5DE799",
                            StrokeColor = DarkMode.IsDarkMode ? "#000000" : "#FFFFFF"
                        },
                        Label = new Label
                        {
                            TextAnchor = TextAnchor.End,
                            OffsetX = -10,
                            Text = $"{(double)(entries.SingleOrDefault(x=>x.Date == now)?.ConsumptionPrice ?? 0)} €c/kWh",
                            Style = new Style
                            {
                                Background = DarkMode.IsDarkMode ? "#000000" : "#FFFFFF",
                                Color = DarkMode.IsDarkMode ? "#FFFFFF" : "#000000"
                            }
                        }
                    },
                    new AnnotationsPoint
                    {
                        X = $"{now:HH:mm}",
                        Y = (double)(entries.SingleOrDefault(x=>x.Date == now)?.InjectionPrice ?? 0),
                        SeriesIndex = 1,
                        Marker = new AnnotationMarker
                        {
                            FillColor = "#FBB550",
                            StrokeColor = DarkMode.IsDarkMode ? "#000000" : "#FFFFFF"
                        },
                        Label = new Label
                        {
                            TextAnchor = TextAnchor.End,
                            OffsetX = -10,
                            Text = $"{(double)(entries.SingleOrDefault(x=>x.Date == now)?.InjectionPrice ?? 0)} €c/kWh",
                            Style = new Style
                            {
                                Background = DarkMode.IsDarkMode ? "#000000" : "#FFFFFF",
                                Color = DarkMode.IsDarkMode ? "#FFFFFF" : "#000000"
                            }
                        }
                    }
                }
            };

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