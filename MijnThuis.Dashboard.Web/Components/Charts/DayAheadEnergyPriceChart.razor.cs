using ApexCharts;
using MediatR;
using MijnThuis.Application.Power.Queries;
using MijnThuis.Contracts.Power;
using MijnThuis.Dashboard.Web.Model.Charts;
using System.Globalization;

namespace MijnThuis.Dashboard.Web.Components.Charts;

public partial class DayAheadEnergyPriceChart
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromHours(1));
    private ApexChart<ChartDataEntry<string, decimal>> _apexChart = null!;
    private ApexChartOptions<ChartDataEntry<string, decimal>> _options { get; set; } = new();

    private ChartData3<string, decimal> DayAheadEnergyPrices { get; set; } = new();
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
                    Formatter = @"function (value) { return value + ' €c/kWh'; }"
                }
            }
        };
        _options.Theme = new Theme
        {
            Mode = Mode.Dark,
            Palette = PaletteType.Palette1
        };
        _options.Colors = new List<string> { "#B0D8FD", "#5DE799", "#FBB550" };
        _options.Stroke = new Stroke
        {
            Curve = Curve.Smooth
        };
        _options.Fill = new Fill
        {
            Type = new List<FillType> { FillType.Solid, FillType.Solid, FillType.Solid },
            Opacity = new Opacity(1, 1, 1)
        };

        DayAheadEnergyPrices.Description = "Elektriciteit: Dynamische tarieven";
        DayAheadEnergyPrices.Series1Description = "Dynamische tarieven";
        DayAheadEnergyPrices.Series2Description = "Mega tarieven voor consumptie";
        DayAheadEnergyPrices.Series3Description = "Mega tarieven voor injectie";
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

            var response = await mediator.Send(new GetDayAheadEnergyPricesQuery
            {
                Date = _selectedDate
            });

            var entries = response.Entries;

            DayAheadEnergyPrices.Clear();
            DayAheadEnergyPrices.Series1.AddRange(entries.Select(x => new ChartDataEntry<string, decimal>
            {
                XValue = $"{x.Date:t}",
                YValue = Math.Round(x.Price, 2)
            }));
            DayAheadEnergyPrices.Series2.AddRange(entries.Select(x => new ChartDataEntry<string, decimal>
            {
                XValue = $"{x.Date:t}",
                YValue = Math.Round(x.ConsumptionPrice, 2)
            }));
            DayAheadEnergyPrices.Series3.AddRange(entries.Select(x => new ChartDataEntry<string, decimal>
            {
                XValue = $"{x.Date:t}",
                YValue = Math.Round(x.InjectionPrice, 2)
            }));

            TitleDescription = string.Create(CultureInfo.GetCultureInfo("nl-be"), $"Dynamische tarieven voor {_selectedDate:D}");

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
        _selectedDate = _selectedDate.AddDays(-1);

        await RefreshData();
    }

    private async Task NavigateNextCommand()
    {
        _selectedDate = _selectedDate.AddDays(1);

        await RefreshData();
    }

    public void Dispose()
    {
        _periodicTimer.Dispose();
    }
}