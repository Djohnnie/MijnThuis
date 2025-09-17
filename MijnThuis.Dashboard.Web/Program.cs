using ApexCharts;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using MijnThuis.Application.DependencyInjection;
using MijnThuis.Dashboard.Web.Copilot;
using MijnThuis.Dashboard.Web.DependencyInjection;
using MijnThuis.Dashboard.Web.Middleware;
using MijnThuis.Dashboard.Web.Model;
using MijnThuis.Dashboard.Web.Notifications;
using MijnThuis.Dashboard.Web.Tools;
using MudBlazor.Services;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel((context, options) =>
{
    var certificateFilename = context.Configuration.GetValue<string>("CERTIFICATE_FILENAME");
    var certificatePassword = context.Configuration.GetValue<string>("CERTIFICATE_PASSWORD");
    var port = context.Configuration.GetValue<int?>("PORT") ?? 8080;

    if (certificateFilename == null)
    {
        options.Listen(IPAddress.Any, port);
    }
    else
    {
        options.Listen(IPAddress.Any, port, listenOption => listenOption.UseHttps(certificateFilename, certificatePassword));
    }
});

StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

builder.Logging.AddConsole();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddInteractiveServerComponents();
builder.Services.AddSingleton<ExtraPageArguments>();
builder.Services.AddMudServices();
builder.Services.AddApexCharts(e =>
{
    e.GlobalOptions = new ApexChartBaseOptions
    {
        Debug = true,
        Theme = new Theme { Palette = PaletteType.Palette6 }
    };
});
builder.Services.AddApplication();
builder.Services.AddMemoryCache();
builder.Services.AddTransient<ICopilotHelper, CopilotHelper>();
builder.Services.AddScoped<SpeechToTextNotificationService>();
builder.Services.AddNotifyingCascadingValue(new NotifyingDarkMode { IsDarkMode = false });
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<MijnThuisPowerTools>()
    .WithTools<MijnThuisSolarTools>()
    .WithTools<MijnThuisCarTools>()
    .WithTools<MijnThuisHeatingTools>()
    .WithTools<MijnThuisSmartLockTools>();

var app = builder.Build();

var accessKey = app.Configuration.GetValue<string>("SUPER_SECRET_ACCESS_KEY");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseMiddleware<SuperSecretAccessKeyMiddleware>();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapMcp($"{accessKey}/mcp");

app.Run();