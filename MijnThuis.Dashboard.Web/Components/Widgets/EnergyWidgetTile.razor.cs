using ApexCharts;
using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Application.Solar.Queries;
using MijnThuis.Dashboard.Web.Model;
using MijnThuis.Dashboard.Web.Model.Charts;

namespace MijnThuis.Dashboard.Web.Components.Widgets;

public partial class EnergyWidgetTile
{
    [CascadingParameter]
    public NotifyingDarkMode DarkMode { get; set; }

    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromMinutes(15));
    private ApexChart<ChartDataEntry<string, decimal>> _productionChart = null!;
    private ApexChart<ChartDataEntry<string, decimal>> _consumptionChart = null!;
    private ApexChartOptions<ChartDataEntry<string, decimal>> _productionOptions { get; set; } = new();
    private ApexChartOptions<ChartDataEntry<string, decimal>> _consumptionOptions { get; set; } = new();

    private ChartData3<string, decimal> Production { get; set; } = new();
    private ChartData3<string, decimal> Consumption { get; set; } = new();

    public EnergyWidgetTile()
    {
        _productionOptions.Chart = new Chart
        {
            Height = "125px",
            Stacked = true,
            StackType = StackType.Percent100,
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
        _productionOptions.PlotOptions = new PlotOptions
        {
            Bar = new PlotOptionsBar
            {
                Horizontal = true
            }
        };
        _productionOptions.Theme = new Theme
        {
            Mode = Mode.Dark,
            Palette = PaletteType.Palette1
        };
        _productionOptions.Colors = new List<string> { "#B6FED6", "#5DE799", "#59C7D4" };
        _productionOptions.Fill = new Fill
        {
            Type = new List<FillType> { FillType.Solid, FillType.Solid, FillType.Solid },
            Opacity = new Opacity(1, 1, 1)
        };
        _productionOptions.Title = new Title
        {
            Floating = true
        };
        _productionOptions.Grid = new Grid
        {
            Show = false,
            Padding = new Padding { Top = 0, Bottom = 0 }
        };
        _productionOptions.Yaxis = [new YAxis {
            Show = false,
            Labels = new YAxisLabels
            {
                Formatter = @"function (value) { return value + ' kWh'; }"
            }
        }];
        _productionOptions.Xaxis = new XAxis
        {
            AxisBorder = new AxisBorder { Show = false },
            AxisTicks = new AxisTicks { Show = false },
            Labels = new XAxisLabels { Show = false }
        };

        _consumptionOptions.Chart = new Chart
        {
            Height = "125px",
            Stacked = true,
            StackType = StackType.Percent100,
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
        _consumptionOptions.PlotOptions = new PlotOptions
        {
            Bar = new PlotOptionsBar
            {
                Horizontal = true,
            }
        };
        _consumptionOptions.Theme = new Theme
        {
            Mode = Mode.Dark,
            Palette = PaletteType.Palette1
        };
        _consumptionOptions.Colors = new List<string> { "#B0D8FD", "#93B6FB", "#FBB550" };
        _consumptionOptions.Fill = new Fill
        {
            Type = new List<FillType> { FillType.Solid, FillType.Solid, FillType.Solid },
            Opacity = new Opacity(1, 1, 1)
        };
        _consumptionOptions.Title = new Title
        {
            Floating = true
        };
        _consumptionOptions.Grid = new Grid
        {
            Show = false
        };
        _consumptionOptions.Yaxis = [new YAxis {
            Show = false,
            Labels = new YAxisLabels
            {
                Formatter = @"function (value) { return value + ' kWh'; }"
            }
        }];
        _consumptionOptions.Xaxis = new XAxis
        {
            AxisBorder = new AxisBorder { Show = false },
            AxisTicks = new AxisTicks { Show = false },
            Labels = new XAxisLabels { Show = false }
        };

        Production.Description = "Productie";
        Production.Series1Description = "Naar huis";
        Production.Series2Description = "Naar batterij";
        Production.Series3Description = "Naar net";
        Consumption.Description = "Consumptie";
        Consumption.Series1Description = "Vanuit PV";
        Consumption.Series2Description = "Vanuit batterij";
        Consumption.Series3Description = "Van net";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (DarkMode != null)
            {
                _productionOptions.Chart.Background = _consumptionOptions.Chart.Background = DarkMode.IsDarkMode ? "#373740" : "#FFFFFF";
                _productionOptions.Theme.Mode = _consumptionOptions.Theme.Mode = DarkMode.IsDarkMode ? Mode.Dark : Mode.Light;
                await _productionChart.UpdateOptionsAsync(true, false, false);
                await _consumptionChart.UpdateOptionsAsync(true, false, false);

                DarkMode.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(DarkMode.IsDarkMode))
                    {
                        _productionOptions.Chart.Background = _consumptionOptions.Chart.Background = DarkMode.IsDarkMode ? "#373740" : "#FFFFFF";
                        _productionOptions.Theme.Mode = _consumptionOptions.Theme.Mode = DarkMode.IsDarkMode ? Mode.Dark : Mode.Light;
                        _ = _productionChart.UpdateOptionsAsync(true, false, false);
                        _ = _consumptionChart.UpdateOptionsAsync(true, false, false);
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

            var response = await mediator.Send(new GetSolarProductionAndConsumptionTodayQuery());

            Production.Clear();
            Production.Series1.AddRange(new ChartDataEntry<string, decimal> { XValue = "", YValue = Math.Round(response.ProductionToHome, 2) });
            Production.Series2.AddRange(new ChartDataEntry<string, decimal> { XValue = "", YValue = Math.Round(response.ProductionToBattery, 2) });
            Production.Series3.AddRange(new ChartDataEntry<string, decimal> { XValue = "", YValue = Math.Round(response.ProductionToGrid, 2) });
            Production.Description = $"Productie ({Math.Round(response.Production, 2):F2} kWh)";
            Production.Series1Description = $"Naar huis ({Math.Round(response.ProductionToHome, 2):F2} kWh)";
            Production.Series2Description = $"Naar batterij ({Math.Round(response.ProductionToBattery, 2):F2} kWh)";
            Production.Series3Description = $"Naar net ({Math.Round(response.ProductionToGrid, 2):F2} kWh)";
            Consumption.Clear();
            Consumption.Series1.AddRange(new ChartDataEntry<string, decimal> { XValue = "", YValue = Math.Round(response.ConsumptionFromSolar, 2) });
            Consumption.Series2.AddRange(new ChartDataEntry<string, decimal> { XValue = "", YValue = Math.Round(response.ConsumptionFromBattery, 2) });
            Consumption.Series3.AddRange(new ChartDataEntry<string, decimal> { XValue = "", YValue = Math.Round(response.ConsumptionFromGrid, 2) });
            Consumption.Description = $"Consumptie ({Math.Round(response.Consumption, 2):F2} kWh)";
            Consumption.Series1Description = $"Vanuit PV ({Math.Round(response.ConsumptionFromSolar, 2):F2} kWh)";
            Consumption.Series2Description = $"Vanuit batterij ({Math.Round(response.ConsumptionFromBattery, 2):F2} kWh)";
            Consumption.Series3Description = $"Van net ({Math.Round(response.ConsumptionFromGrid, 2):F2} kWh)";

            await InvokeAsync(StateHasChanged);
            await Task.Delay(100);
            await _productionChart.UpdateSeriesAsync(false);
            await _productionChart.UpdateOptionsAsync(true, false, false);
            await _consumptionChart.UpdateSeriesAsync(false);
            await _consumptionChart.UpdateOptionsAsync(true, false, false);
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
}