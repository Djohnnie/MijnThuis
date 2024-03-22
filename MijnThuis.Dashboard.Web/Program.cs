using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using MijnThuis.Application.DependencyInjection;
using MijnThuis.Dashboard.Web.Middleware;
using MudBlazor.Services;
#if !DEBUG
using System.Net;
#endif

var builder = WebApplication.CreateBuilder(args);

#if !DEBUG
builder.WebHost.ConfigureKestrel((context, options) =>
{
    var certificateFilename = context.Configuration.GetValue<string>("CERTIFICATE_FILENAME");
    var certificatePassword = context.Configuration.GetValue<string>("CERTIFICATE_PASSWORD");

    if (certificateFilename == null)
    {
        options.Listen(IPAddress.Any, 5000);
    }
    else
    {
        options.Listen(IPAddress.Any, 5001, listenOption => listenOption.UseHttps(certificateFilename, certificatePassword));
    }
});
#endif

StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

builder.Logging.AddConsole();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddInteractiveServerComponents();
builder.Services.AddSingleton<ExtraPageArguments>();
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