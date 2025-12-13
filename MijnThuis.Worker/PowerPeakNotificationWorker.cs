using MijnThuis.Integrations.Power;
using MijnThuis.Integrations.Solar;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace MijnThuis.Worker;

internal class PowerPeakNotificationWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceProvider;
    private readonly ILogger<PowerPeakNotificationWorker> _logger;

    public PowerPeakNotificationWorker(
        IConfiguration configuration,
        IServiceScopeFactory serviceProvider,
        ILogger<PowerPeakNotificationWorker> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mailgunBaseAddress = _configuration.GetValue<string>("MAILGUN_BASE_ADDRESS");
        var mailgunDomain = _configuration.GetValue<string>("MAILGUN_DOMAIN");
        var mailgunApiKey = _configuration.GetValue<string>("MAILGUN_APIKEY");
        var mailgunSender = _configuration.GetValue<string>("MAILGUN_SENDER");
        var mailgunReceivers = _configuration.GetValue<string>("MAILGUN_RECEIVERS");

        var lastNotificationTime = DateTime.MinValue;
        var consecutiveHighUsageCount = 0;

        // While the service is not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Use a timestamp to calculate the duration of the whole process.
                var startTimer = Stopwatch.GetTimestamp();

                // Initialize dependencies and variables.
                using var serviceScope = _serviceProvider.CreateScope();
                var powerService = serviceScope.ServiceProvider.GetRequiredService<IPowerService>();
                var modbusService = serviceScope.ServiceProvider.GetRequiredService<IModbusService>();

                // Get information about power usage.
                var powerOverview = await powerService.GetOverview();

                if (powerOverview.CurrentPower > 2500)
                {
                    consecutiveHighUsageCount++;

                    if (consecutiveHighUsageCount >= 3)
                    {
                        // Disable battery charging if it is active.
                        await modbusService.StopChargingBattery();
                    }

                    if (consecutiveHighUsageCount >= 3 && (DateTime.Now - lastNotificationTime).TotalMinutes > 5)
                    {
                        consecutiveHighUsageCount = 0;
                        lastNotificationTime = DateTime.Now;

                        await SendEmail($"Er is momenteel een hoog verbruik van {powerOverview.CurrentPower}W!",
                            mailgunBaseAddress, mailgunDomain, mailgunSender, mailgunReceivers, mailgunApiKey);
                    }
                }
                else
                {
                    // Reset the counter if usage is below threshold.
                    consecutiveHighUsageCount = 0;
                }

                // Calculate the duration for this whole process.
                var stopTimer = Stopwatch.GetTimestamp();

                // Wait for a maximum of 10 seconds before the next iteration.
                var duration = TimeSpan.FromSeconds(10) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

                if (duration > TimeSpan.Zero)
                {
                    await Task.Delay(duration, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong: {ex.Message}");
                _logger.LogError(ex, ex.Message);

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task SendEmail(string message, string baseAddress, string domain, string sender, string receivers, string apiKey)
    {
        var subject = "MijnThuis - Piekverbruik notificatie";

        var client = new HttpClient();
        client.BaseAddress = new Uri(baseAddress);
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{apiKey}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
        var content = new MultipartFormDataContent
        {
            { new StringContent(sender), "from" },
            { new StringContent(receivers), "to" },
            { new StringContent(subject), "subject" },
            { new StringContent(message), "text" },
        };

        await client.PostAsync($"/v3/{domain}/messages", content);
    }
}