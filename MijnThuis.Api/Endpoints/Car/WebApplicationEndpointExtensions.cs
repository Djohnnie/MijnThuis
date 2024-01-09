using MijnThuis.Contracts.Car;

namespace MijnThuis.Api.Endpoints.Car;

public static class WebApplicationEndpointExtensions
{
    public static void MapCarEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        var api = endpointRouteBuilder.MapGroup("/car").WithName("Car").WithTags("Car");
        api.MapGetCarOverview();
    }

    public static void MapGetCarOverview(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("/overview", () =>
        {
            return new GetCarOverviewResponse
            {
                State = "Parked",
                BatteryLevel = 55,
                RemainingRange = 204,
                Location = "Veldkant 33C, 2550 Kontich"
            };

        }).WithName("GetCarOverview").WithOpenApi();
    }
}