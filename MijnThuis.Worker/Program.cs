using MijnThuis.DataAccess.DependencyInjection;
using MijnThuis.Integrations.DependencyInjection;
using MijnThuis.Worker;
using MijnThuis.Worker.Helpers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDataAccess();
builder.Services.AddIntegrations();

builder.Services.AddScoped<ICarChargingHelper, CarChargingHelper>();
builder.Services.AddScoped<IHomeBatteryChargingHelper, HomeBatteryChargingHelper>();

builder.Services.AddHostedService<CarChargingWorker>();
//builder.Services.AddHostedService<HomeBatteryChargingWorker>();
builder.Services.AddHostedService<HomeBatteryNotificationWorker>();
builder.Services.AddHostedService<SolarHistoryWorker>();
builder.Services.AddHostedService<EnergyHistoryWorker>();

var host = builder.Build();

host.Run();