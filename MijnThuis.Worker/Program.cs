using MijnThuis.Integrations.DependencyInjection;
using MijnThuis.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddIntegrations();
builder.Services.AddHostedService<CarChargingWorker>();

var host = builder.Build();
host.Run();