using MijnThuis.Integrations.Solar;
using MudBlazor;

namespace MijnThuis.Dashboard.Web.Pages
{
    public partial class Solar
    {
        private readonly List<ChartSeries> _series = new();
        private readonly ChartOptions _options = new();
        private string[] XAxisLabels { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var solarService = ScopedServices.GetRequiredService<ISolarService>();

            var data = await solarService.GetStorageData(StorageDataRange.Today);

            _series.Add(new ChartSeries { Name = "Battery", Data = data.Entries.Select(x => (double)x.ChargeState).ToArray() });

            _options.YAxisTicks = 10;
            _options.YAxisLines = true;
            _options.DisableLegend = true;
            _options.InterpolationOption = InterpolationOption.Straight;

            XAxisLabels = data.Entries.Select(x =>
            {
                var label = string.Empty;

                return label;
            }).ToArray();

            await base.OnInitializedAsync();
        }
    }
}