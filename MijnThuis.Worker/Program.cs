using MijnThuis.Integrations.DependencyInjection;
using MijnThuis.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddIntegrations();
builder.Services.AddHostedService<CarChargingWorker>();
builder.Services.AddHostedService<HomeBatteryChargingWorker>();
builder.Services.AddHostedService<HomeBatteryNotificationWorker>();

var host = builder.Build();
host.Run();