using MediatR;
using MijnThuis.Contracts.Solar;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace MijnThuis.Dashboard.Web.Tools;

[McpServerToolType]
public class MijnThuisSolarTools
{
    private readonly IMediator _mediator;

    public MijnThuisSolarTools(IMediator mediator)
    {
        _mediator = mediator;
    }

    [McpServerTool(Name = $"mijnthuis_solar_{nameof(GetSolarInformation)}", ReadOnly = true)]
    [Description("Gets information about my solar installation at home like current solar power, current battery power, current grid power, current consumption power, energy generated today and this month, solar energy forecast today and tomorrow, battery percentage and battery health.")]
    [return: Description("Information, formatted in JSON containing the current solar power, current battery power, current grid power, current consumption power, energy generated today and this month, solar energy forecast today and tomorrow, battery percentage and battery health.")]
    public async Task<string> GetSolarInformation()
    {
        var solarInfo = await _mediator.Send(new GetSolarOverviewQuery());
        return JsonSerializer.Serialize(solarInfo);
    }
}