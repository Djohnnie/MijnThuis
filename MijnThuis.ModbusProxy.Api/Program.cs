using MijnThuis.ModbusProxy.Api.Helpers;
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

builder.Services.AddSingleton<IModbusHelper, ModbusHelper>();
builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/bulk", async (IModbusHelper modbusHelper) =>
{
    var result = await modbusHelper.GetBulkDataSet();
    return Results.Ok(result);
});

app.MapGet("/overview", async (IModbusHelper modbusHelper) =>
{
    var result = await modbusHelper.GetOverview();
    return Results.Ok(result);
});

app.MapGet("/battery", async (IModbusHelper modbusHelper) =>
{
    var result = await modbusHelper.GetBatteryLevel();
    return Results.Ok(result);
});

app.Run();