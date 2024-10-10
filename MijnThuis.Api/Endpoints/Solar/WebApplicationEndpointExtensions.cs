using MijnThuis.Contracts.Solar;

namespace MijnThuis.Api.Endpoints.Solar;

public static class WebApplicationEndpointExtensions
{
    public static void MapSolarEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        var api = endpointRouteBuilder.MapGroup("/solar").WithName("Solar").WithTags("Solar");
        api.MapGetSolarOverview();
    }

    private static void MapGetSolarOverview(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("/overview", () =>
        {
            return new GetSolarOverviewResponse
            {
                CurrentBatteryPower = 259,
                BatteryLevel = 34
            };

        }).WithName("GetSolarOverview").WithOpenApi();
    }
}