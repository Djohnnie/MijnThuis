using MijnThuis.Integrations.Solar;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace MijnThuis.Worker;

internal class HomeBatteryNotificationWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceProvider;
    private readonly ILogger<HomeBatteryNotificationWorker> _logger;

    public HomeBatteryNotificationWorker(
        IConfiguration configuration,
        IServiceScopeFactory serviceProvider,
        ILogger<HomeBatteryNotificationWorker> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var emailDetails = new EmailDetails
        {
            BaseAddress = _configuration.GetValue<string>("MAILGUN_BASE_ADDRESS"),
            Domain = _configuration.GetValue<string>("MAILGUN_DOMAIN"),
            ApiKey = _configuration.GetValue<string>("MAILGUN_APIKEY"),
            Sender = _configuration.GetValue<string>("MAILGUN_SENDER"),
            Receivers = _configuration.GetValue<string>("MAILGUN_RECEIVERS"),
        };

        var previousBatteryLevel = -1M;

        // While the service is not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Use a timestamp to calculate the duration of the whole process.
                var startTimer = Stopwatch.GetTimestamp();

                // Initialize dependencies and variables.
                using var serviceScope = _serviceProvider.CreateScope();
                var modbusService = serviceScope.ServiceProvider.GetService<IModbusService>();

                // Get information about solar energy and battery level
                var solarOverview = await modbusService.GetOverview();
                var currentBatteryLevel = solarOverview.BatteryLevel;

                if (currentBatteryLevel == 100 && previousBatteryLevel < 100)
                {
                    await SendEmail("De thuisbatterij is volledig opgeladen (100%)!", emailDetails);
                }

                if (currentBatteryLevel <= 50 && previousBatteryLevel > 50)
                {
                    await SendEmail("De thuisbatterij is nog half vol (50%)!", emailDetails);
                }

                if (currentBatteryLevel <= 20 && previousBatteryLevel > 20)
                {
                    await SendEmail("De thuisbatterij is bijna leeg (< 20%)!", emailDetails);
                }

                if (currentBatteryLevel <= 5 && previousBatteryLevel > 5)
                {
                    await SendEmail("De thuisbatterij is bijna helemaal leeg (< 5%)!", emailDetails);
                }

                if (currentBatteryLevel == 0 && previousBatteryLevel > 0)
                {
                    await SendEmail("De thuisbatterij is helemaal leeg (0%)!", emailDetails);
                }

                if (currentBatteryLevel >= 90 && previousBatteryLevel < 90)
                {
                    await SendEmail("De thuisbatterij is bijna volledig opgeladen (> 90%)!", emailDetails);
                }

                // Remember the battery level for the next iteration.
                previousBatteryLevel = currentBatteryLevel;

                // Calculate the duration for this whole process.
                var stopTimer = Stopwatch.GetTimestamp();

                // Wait for a maximum of 1 minute before the next iteration.
                var duration = TimeSpan.FromMinutes(1) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

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

    private async Task SendEmail(string message, EmailDetails details)
    {
        var subject = "MijnThuis - Thuisbatterij notificatie";

        var client = new HttpClient();
        client.BaseAddress = new Uri(details.BaseAddress);
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{details.ApiKey}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
        var content = new MultipartFormDataContent
        {
            { new StringContent(details.Sender), "from" },
            { new StringContent(details.Receivers), "to" },
            { new StringContent(subject), "subject" },
            { new StringContent(message), "text" },
        };

        await client.PostAsync($"/v3/{details.Domain}/messages", content);
    }

    private class EmailDetails
    {
        public string Message { get; set; }
        public string BaseAddress { get; set; }
        public string Domain { get; set; }
        public string Sender { get; set; }
        public string Receivers { get; set; }
        public string ApiKey { get; set; }
    }
}