using MijnThuis.Contracts.Power;

namespace MijnThuis.Api.Endpoints.Power;

public static class WebApplicationEndpointExtensions
{
    public static void MapPowerEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        var api = endpointRouteBuilder.MapGroup("/power").WithName("Power").WithTags("Power");
        api.MapGetPowerOverview();
    }

    private static void MapGetPowerOverview(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("/overview", () =>
        {
            return new GetPowerOverviewResponse
            {
                CurrentPower = 134
            };

        }).WithName("GetPowerOverview").WithOpenApi();
    }
}