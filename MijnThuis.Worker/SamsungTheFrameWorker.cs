using MijnThuis.DataAccess.Repositories;
using MijnThuis.Integrations.Samsung;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace MijnThuis.Worker;

internal class SamsungTheFrameWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SamsungTheFrameWorker> _logger;

    public SamsungTheFrameWorker(
        IServiceScopeFactory serviceProvider,
        IConfiguration configuration,
        ILogger<SamsungTheFrameWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // While the service is not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            // Use a timestamp to calculate the duration of the whole process.
            var startTimer = Stopwatch.GetTimestamp();

            try
            {
                using var serviceScope = _serviceProvider.CreateScope();
                var samsungService = serviceScope.ServiceProvider.GetRequiredService<ISamsungService>();
                var flagRepository = serviceScope.ServiceProvider.GetRequiredService<IFlagRepository>();

                var flag = await flagRepository.GetSamsungTheFrameTokenFlag();

                if (!flag.IsDisabled)
                {
                    var isOn = await samsungService.IsTheFrameOn();
                    var now = DateTime.Now;
                    var today = DateTime.Today;

                    if (!isOn && now > today.Add(flag.AutoOn) && now < today.Add(flag.AutoOff))
                    {
                        await samsungService.TurnOnTheFrame(flag.Token);
                        _logger.LogInformation($"Samsung The Frame TV turned on at {flag.AutoOn:hh\\:mm}.");
                        await Task.Delay(10000, stoppingToken);

                        break;
                    }

                    if (isOn && (now < today.Add(flag.AutoOn) || now > today.Add(flag.AutoOff)))
                    {
                        await samsungService.TurnOffTheFrame(flag.Token);
                        _logger.LogInformation($"Samsung The Frame TV turned off at {flag.AutoOff:hh\\:mm}.");
                        await Task.Delay(10000, stoppingToken);

                        break;
                    }

                    var lastPingFlag = await GetLastPingFlag();
                    var shouldBeOn = now > today.Add(flag.AutoOn) && now < today.Add(flag.AutoOff);
                    var lastPingLongTimeAgo = DateTime.Now > lastPingFlag.LastPing.AddMinutes(5);
                    if (isOn && shouldBeOn && lastPingLongTimeAgo)
                    {
                        await samsungService.TurnOffTheFrame(flag.Token);
                        await Task.Delay(10000, stoppingToken);
                        await samsungService.TurnOnTheFrame(flag.Token);
                        await Task.Delay(10000, stoppingToken);
                        _logger.LogInformation($"Samsung The Frame TV restarted at {DateTime.Now:hh\\:mm}.");
                        await Task.Delay(10000, stoppingToken);

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Something went wrong: {ex.Message}");
                _logger.LogError(ex, ex.Message);
            }

            // Calculate the duration for this whole process.
            var stopTimer = Stopwatch.GetTimestamp();

            // Wait for 5 minutes before the next iteration.
            var duration = TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

            if (duration > TimeSpan.Zero)
            {
                await Task.Delay(duration, stoppingToken);
            }
        }
    }

    private async Task<DisplayPingFlag> GetLastPingFlag()
    {
        var baseUrl = _configuration.GetValue<string>("PHOTO_CAROUSEL_API_BASE_ADDRESS");
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(baseUrl);
        return await httpClient.GetFromJsonAsync<DisplayPingFlag>("flags/DisplayPingFlag");
    }
}

public class DisplayPingFlag
{
    public const string Name = "DisplayPingFlag";

    public static DisplayPingFlag Default => new DisplayPingFlag();

    public DateTime LastPing { get; set; }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static DisplayPingFlag Deserialize(string json)
    {
        return JsonSerializer.Deserialize<DisplayPingFlag>(json);
    }
}