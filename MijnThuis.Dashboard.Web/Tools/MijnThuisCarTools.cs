using MediatR;
using MijnThuis.Contracts.Car;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace MijnThuis.Dashboard.Web.Tools;

[McpServerToolType]
public class MijnThuisCarTools
{
    private readonly Mediator _mediator;

    public MijnThuisCarTools(Mediator mediator)
    {
        _mediator = mediator;
    }

    [McpServerTool]
    [Description("Gets information about my electric car like location, battery level and health, charging statistics, temperature and charging possibilities.")]
    [return: Description("Information, formatted in JSON containing the car location, battery level and health, charging statistics, temperature and charging possibilities.")]
    public async Task<string> GetCarInformation()
    {
        var carInfo = await _mediator.Send(new GetCarOverviewQuery());
        return JsonSerializer.Serialize(carInfo);
    }
}