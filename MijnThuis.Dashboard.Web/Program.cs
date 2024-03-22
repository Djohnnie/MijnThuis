using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using MijnThuis.Application.DependencyInjection;
using MijnThuis.Dashboard.Web.Data;
using MijnThuis.Dashboard.Web.Middleware;
using MudBlazor.Services;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel((context, options) =>
{
    var certificateFilename = context.Configuration.GetValue<string>("CERTIFICATE_FILENAME");
    var certificatePassword = context.Configuration.GetValue<string>("CERTIFICATE_PASSWORD");

    if (certificateFilename == null)
    {
        options.Listen(IPAddress.Any, 8080);
    }
    else
    {
        options.Listen(IPAddress.Any, 8080, listenOption => listenOption.UseHttps(certificateFilename, certificatePassword));
    }
});

StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

builder.Logging.AddConsole();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddInteractiveServerComponents();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddMudServices();
builder.Services.AddApplication();
builder.Services.AddMemoryCache();

var app = builder.Build();

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

app.Run();