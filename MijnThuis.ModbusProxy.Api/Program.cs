using Microsoft.AspNetCore.Mvc;
using MijnThuis.ModbusProxy.Api.Helpers;
using MijnThuis.ModbusProxy.Api.Middleware;
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
app.UseMiddleware<ClientSecretMiddleware>();

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

app.MapPost("/battery/startCharging", async (IModbusHelper modbusHelper, [FromQuery] int durationInMinutes, [FromQuery] int power) =>
{
    if (durationInMinutes is <= 0 or > 480)
        return Results.BadRequest("durationInMinutes must be between 1 and 480.");

    if (power is <= 0 or > 5000)
        return Results.BadRequest("power must be between 1 and 5000 W.");

    await modbusHelper.StartChargingBattery(TimeSpan.FromMinutes(durationInMinutes), power);
    return Results.Ok();
});

app.MapPost("/battery/stopCharging", async (IModbusHelper modbusHelper) =>
{
    await modbusHelper.StopChargingBattery();
    return Results.Ok();
});

app.MapGet("/hasExportLimitation", async (IModbusHelper modbusHelper) =>
{
    var result = await modbusHelper.HasExportLimitation();
    return Results.Ok(result);
});

app.MapPost("/setExportLimitation", async (IModbusHelper modbusHelper, [FromQuery] float powerLimit) =>
{
    if (powerLimit is < 0 or > 10000)
        return Results.BadRequest("powerLimit must be between 0 and 10000 W.");

    await modbusHelper.SetExportLimitation(powerLimit);
    return Results.Ok();
});

app.MapPost("/resetExportLimitation", async (IModbusHelper modbusHelper) =>
{
    await modbusHelper.ResetExportLimitation();
    return Results.Ok();
});

app.Run();