using MijnThuis.ModbusProxy.Api.Helpers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IModbusHelper, ModbusHelper>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/status", async (IModbusHelper modbusHelper) =>
{
    var result = await modbusHelper.GetBulkDataSet();
    return Results.Ok(result);
});

app.Run();