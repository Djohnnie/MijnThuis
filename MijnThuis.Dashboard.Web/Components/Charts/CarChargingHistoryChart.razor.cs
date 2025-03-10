using ApexCharts;
using MediatR;
using MijnThuis.Contracts.Car;
using MijnThuis.Contracts.Solar;
using MijnThuis.Dashboard.Web.Model.Charts;

namespace MijnThuis.Dashboard.Web.Components.Charts;

public partial class CarChargingHistoryChart
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromMinutes(15));
    private ApexChart<ChartDataEntry<string, decimal>> _apexChart = null!;
    private ApexChartOptions<ChartDataEntry<string, decimal>> _options { get; set; } = new();

    private ChartData1<string, decimal> ChargingHistory { get; set; } = new();

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
            Background = "#373740",
        };
        _options.Xaxis = new XAxis
        {
            Type = XAxisType.Category,
            OverwriteCategories = ["Jan", "Feb", "Maa", "Apr", "Mei", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dec"],
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

        ChargingHistory.Description = $"Auto opladen uit zonneënergie";
        ChargingHistory.Series1Description = $"Opladen {DateTime.Today.Year}";
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

            var response = await mediator.Send(new GetCarChargingHistoryQuery
            {
                From = new DateTime(DateTime.Today.Year, 1, 1),
                To = new DateTime(DateTime.Today.Year, 12, 31),
                Unit = EnergyHistoryUnit.Month
            });

            ChargingHistory.Clear();
            ChargingHistory.Series1.AddRange(FillData(response.Entries
                .Select(x => new ChartDataEntry<string, decimal>
                {
                    XValue = $"{x.Date:MMMM yyyy}",
                    YValue = Math.Round(x.EnergyCharged / 1000, 2)
                }), 12, n => $"{new DateTime(DateTime.Today.Year, n, 1):MMMM yyyy}"));
            await _apexChart.UpdateSeriesAsync(true);

            await InvokeAsync(StateHasChanged);
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