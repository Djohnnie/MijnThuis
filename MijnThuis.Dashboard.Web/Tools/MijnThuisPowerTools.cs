using MediatR;
using MijnThuis.Contracts.Power;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace MijnThuis.Dashboard.Web.Tools;

[McpServerToolType]
public class MijnThuisPowerTools
{
    private readonly IMediator _mediator;

    public MijnThuisPowerTools(IMediator mediator)
    {
        _mediator = mediator;
    }

    [McpServerTool(Name = $"mijnthuis_power_{nameof(GetPowerInformation)}", ReadOnly = true)]
    [Description("Gets information about my power usage at home like current power usage, power peak this month, imported energy today and this month, exported energy today and this month, energy cost today and this month, current consumption price and current injection price.")]
    [return: Description("Information, formatted in JSON containing the current power usage, power peak this month, imported energy today and this month, exported energy today and this month, energy cost today and this month, current consumption price and current injection price.")]
    public async Task<string> GetPowerInformation()
    {
        var powerInfo = await _mediator.Send(new GetPowerOverviewQuery());
        return JsonSerializer.Serialize(powerInfo);
    }
}