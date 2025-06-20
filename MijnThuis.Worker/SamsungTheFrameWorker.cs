using MijnThuis.DataAccess.Repositories;
using MijnThuis.Integrations.Samsung;
using System.Diagnostics;

namespace MijnThuis.Worker;

internal class SamsungTheFrameWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceProvider;
    private readonly ILogger<SamsungTheFrameWorker> _logger;

    public SamsungTheFrameWorker(
        IServiceScopeFactory serviceProvider,
        ILogger<SamsungTheFrameWorker> logger)
    {
        _serviceProvider = serviceProvider;
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

                var isOn = await samsungService.IsTheFrameOn();
                var now = DateTime.Now;
                var today = DateTime.Today;

                if (!isOn && now > today.Add(flag.AutoOn) && now < today.Add(flag.AutoOff))
                {
                    await samsungService.TurnOnTheFrame(flag.Token);
                    _logger.LogInformation($"Samsung The Frame TV turned on at {flag.AutoOn:hh\\:mm}.");
                }

                if (isOn && (now < today.Add(flag.AutoOn) || now > today.Add(flag.AutoOff)))
                {
                    await samsungService.TurnOffTheFrame(flag.Token);
                    _logger.LogInformation($"Samsung The Frame TV turned off at {flag.AutoOff:hh\\:mm}.");
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
}