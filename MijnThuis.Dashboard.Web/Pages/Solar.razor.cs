using ApexCharts;
using MediatR;
using Microsoft.AspNetCore.Components;
using MijnThuis.Contracts.Solar;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Dashboard.Web.Pages;

public partial class Solar
{
    [Inject]
    protected NavigationManager NavigationManager { get; set; }

    private ApexChart<SolarEnergyHistoryEntry> _apexChart = null!;
    private ApexChart<SolarPowerHistoryEntry> _apexChart2 = null!;
    private ApexChartOptions<SolarEnergyHistoryEntry> _options { get; set; } = new();
    private ApexChartOptions<SolarPowerHistoryEntry> _options2 { get; set; } = new();

    private List<SolarPowerHistoryEntry> SolarPowerYesterday { get; set; } = new();
    private List<SolarEnergyHistoryEntry> Data { get; set; } = [];


    protected override async Task OnInitializedAsync()
    {
        _options2.Chart = new Chart
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
            Background = "#373740"
        };
        _options2.Theme = new Theme
        {
            Mode = Mode.Dark,
            Palette = PaletteType.Palette1
        };
        _options2.Colors = new List<string> { "#B6FED6", "#5DE799", "#59C7D4" };
        _options2.Stroke = new Stroke
        {
            Show = false
        };
        _options2.Fill = new Fill
        {
            Type = new List<FillType> { FillType.Solid, FillType.Solid, FillType.Solid },
            Opacity = new Opacity(1, 1, 1)
        };

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
            Background = "#373740"
        };
        _options.Theme = new Theme
        {
            Mode = Mode.Dark,
            Palette = PaletteType.Palette1
        };
        _options.DataLabels = new DataLabels
        {
            Enabled = true
        };

        _options.Xaxis = new XAxis
        {
            Type = XAxisType.Category,
            Categories = new List<string> { "Jan", "Feb", "Maa", "Apr", "Mei", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dec" }
        };
        _options.Yaxis = new List<YAxis>{
            new YAxis
            {
                DecimalsInFloat = 0,
                Title = new AxisTitle
                {
                    Text = "kWh"
                },
                ForceNiceScale = true
            }
        };

        Data = new List<SolarEnergyHistoryEntry>()
        {
            new SolarEnergyHistoryEntry
            {
                Date = new DateTime(2024, 1, 1),
                ConsumptionFromSolar = 11
            }
        };

        await RefreshData(StorageDataRange.Today);

        await base.OnInitializedAsync();
    }

    private async Task RefreshData(StorageDataRange dataRange)
    {
        var solarService = ScopedServices.GetRequiredService<ISolarService>();

        var data = await solarService.GetStorageData(dataRange);

        var mediator = ScopedServices.GetRequiredService<IMediator>();
        var solarEnergyByMonth = await mediator.Send(new GetSolarEnergyHistoryQuery
        {
            From = new DateTime(2024, 1, 1),
            To = new DateTime(2024, 12, 31),
            Unit = EnergyHistoryUnit.Month
        });
        var solarEnergyByYear = await mediator.Send(new GetSolarEnergyHistoryQuery
        {
            From = new DateTime(DateTime.Today.Year - 2, 1, 1),
            To = new DateTime(DateTime.Today.Year, 12, 31),
            Unit = EnergyHistoryUnit.Year
        });
        var solarPowerByFifteenMinutes = await mediator.Send(new GetSolarPowerHistoryQuery
        {
            From = new DateTime(2024, 8, 10),
            To = new DateTime(2024, 8, 10),
            Unit = PowerHistoryUnit.FifteenMinutes
        });

        SolarPowerYesterday.Clear();
        SolarPowerYesterday.AddRange(solarPowerByFifteenMinutes.Entries);
        await _apexChart2.UpdateSeriesAsync(true);

        Data.Clear();
        Data.AddRange(solarEnergyByMonth.Entries);
        await _apexChart.UpdateSeriesAsync(true);

        //_series.Clear();
        //_series.Add(new ChartSeries { Name = "Battery", Data = solarPowerByFifteenMinutes.Entries.Select(x => (double)x.StorageLevel).ToArray() });


        //_options2.YAxisTicks = 50;
        //_options2.YAxisLines = true;

        //_series2.Clear();
        //_series2.Add(new ChartSeries { Name = "Productie naar het huis", Data = solarEnergyByMonth.Entries.Select(x => (double)x.ProductionToHome).ToArray() });
        //_series2.Add(new ChartSeries { Name = "Productie naar de batterij", Data = solarEnergyByMonth.Entries.Select(x => (double)x.ProductionToBattery).ToArray() });
        //_series2.Add(new ChartSeries { Name = "Productie naar het net", Data = solarEnergyByMonth.Entries.Select(x => (double)x.ProductionToGrid).ToArray() });

        //_options3.YAxisTicks = 50;
        //_options3.YAxisLines = true;

        //_series3.Clear();
        //_series3.Add(new ChartSeries { Name = "Consumptie van de zon", Data = solarEnergyByMonth.Entries.Select(x => (double)x.ConsumptionFromSolar).ToArray() });
        //_series3.Add(new ChartSeries { Name = "Consumptie van de batterij", Data = solarEnergyByMonth.Entries.Select(x => (double)x.ConsumptionFromBattery).ToArray() });
        //_series3.Add(new ChartSeries { Name = "Consumptie van het net", Data = solarEnergyByMonth.Entries.Select(x => (double)x.ConsumptionFromGrid).ToArray() });

        //_series4.Clear();
        //_series4.Add(new ChartSeries { Name = "Import", Data = solarEnergyByMonth.Entries.Select(x => (double)x.Import).ToArray() });
        //_series4.Add(new ChartSeries { Name = "Export", Data = solarEnergyByMonth.Entries.Select(x => (double)x.Export).ToArray() });


        //_seriesA.Clear();
        //_seriesA.Add(new ChartSeries { Name = "Productie naar het huis", Data = solarPowerByFifteenMinutes.Entries.Select(x => (double)x.ProductionToHome).ToArray() });
        //_seriesA.Add(new ChartSeries { Name = "Productie naar de batterij", Data = solarPowerByFifteenMinutes.Entries.Select(x => (double)x.ProductionToBattery).ToArray() });
        //_seriesA.Add(new ChartSeries { Name = "Productie naar het net", Data = solarPowerByFifteenMinutes.Entries.Select(x => (double)x.ProductionToGrid).ToArray() });

        await InvokeAsync(StateHasChanged);
    }

    public void BackCommand()
    {
        NavigationManager.NavigateTo($"/{new Uri(NavigationManager.Uri).Query}");
    }

    public async Task BatteryTodayCommand()
    {
        await RefreshData(StorageDataRange.Today);
    }

    public async Task BatteryThreeDaysCommand()
    {
        await RefreshData(StorageDataRange.ThreeDays);
    }

    public async Task BatteryWeekCommand()
    {
        await RefreshData(StorageDataRange.Week);
    }

    public async Task BatteryMonthCommand()
    {
        await RefreshData(StorageDataRange.Month);
    }
}