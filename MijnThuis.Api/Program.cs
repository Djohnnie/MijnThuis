using MijnThuis.Api.Endpoints.Car;
using MijnThuis.Api.Endpoints.Power;
using MijnThuis.Api.Endpoints.Solar;

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.UseKestrelHttpsConfiguration();

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

var api = app.MapGroup("/api").WithName("Api");
api.MapCarEndpoints();
api.MapPowerEndpoints();
api.MapSolarEndpoints();

app.Run();