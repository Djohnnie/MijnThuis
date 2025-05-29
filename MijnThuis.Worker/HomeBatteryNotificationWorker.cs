using MijnThuis.Integrations.Solar;
using SendGrid;
using SendGrid.Helpers.Mail;
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
        var mailgunBaseAddress = _configuration.GetValue<string>("MAILGUN_BASE_ADDRESS");
        var mailgunDomain = _configuration.GetValue<string>("MAILGUN_DOMAIN");
        var mailgunApiKey = _configuration.GetValue<string>("MAILGUN_APIKEY");
        var mailgunSender = _configuration.GetValue<string>("MAILGUN_SENDER");
        var mailgunReceivers = _configuration.GetValue<string>("MAILGUN_RECEIVERS");

        DateOnly? notifiedFullBatteryToday = null;
        DateOnly? notifiedLowBatteryToday = null;
        DateOnly? notifiedEmptyBatteryToday = null;

        // While the service is not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Use a timestamp to calculate the duration of the whole process.
                var startTimer = Stopwatch.GetTimestamp();

                if (notifiedFullBatteryToday.HasValue && notifiedFullBatteryToday.Value != DateOnly.FromDateTime(DateTime.Today))
                {
                    notifiedFullBatteryToday = null;
                }

                if (notifiedLowBatteryToday.HasValue && notifiedLowBatteryToday.Value != DateOnly.FromDateTime(DateTime.Today))
                {
                    notifiedLowBatteryToday = null;
                }

                if (notifiedEmptyBatteryToday.HasValue && notifiedEmptyBatteryToday.Value != DateOnly.FromDateTime(DateTime.Today))
                {
                    notifiedEmptyBatteryToday = null;
                }

                // Initialize dependencies and variables.
                using var serviceScope = _serviceProvider.CreateScope();
                var modbusService = serviceScope.ServiceProvider.GetService<IModbusService>();

                // Get information about solar energy.
                var solarOverview = await modbusService.GetOverview();

                if (solarOverview.BatteryLevel == 100 && notifiedFullBatteryToday == null)
                {
                    notifiedFullBatteryToday = DateOnly.FromDateTime(DateTime.Today);
                    await SendEmail("De thuisbatterij is volledig opgeladen (100%)!",
                        mailgunBaseAddress, mailgunDomain, mailgunSender, mailgunReceivers, mailgunApiKey);
                }

                if (solarOverview.BatteryLevel < 20 && notifiedLowBatteryToday == null)
                {
                    notifiedLowBatteryToday = DateOnly.FromDateTime(DateTime.Today);
                    await SendEmail("De thuisbatterij is bijna leeg (< 20%)!",
                        mailgunBaseAddress, mailgunDomain, mailgunSender, mailgunReceivers, mailgunApiKey);
                }

                if (solarOverview.BatteryLevel == 0 && notifiedEmptyBatteryToday == null)
                {
                    notifiedEmptyBatteryToday = DateOnly.FromDateTime(DateTime.Today);
                    await SendEmail("De thuisbatterij is helemaal leeg (0%)!",
                        mailgunBaseAddress, mailgunDomain, mailgunSender, mailgunReceivers, mailgunApiKey);
                }

                // Calculate the duration for this whole process.
                var stopTimer = Stopwatch.GetTimestamp();

                // Wait for a maximum of 5 minutes before the next iteration.
                var duration = TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

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
        var subject = "MijnThuis - Thuisbatterij notificatie";

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