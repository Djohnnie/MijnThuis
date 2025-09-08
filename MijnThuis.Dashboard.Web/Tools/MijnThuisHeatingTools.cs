using MediatR;
using MijnThuis.Contracts.Heating;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace MijnThuis.Dashboard.Web.Tools;

[McpServerToolType]
public class MijnThuisHeatingTools
{
    private readonly IMediator _mediator;

    public MijnThuisHeatingTools(IMediator mediator)
    {
        _mediator = mediator;
    }

    [McpServerTool(Name = $"mijnthuis_heating_{nameof(GetHeatingInformation)}", ReadOnly = true)]
    [Description("Gets information about my heating at home like mode, living room temperature, outdoor temperature, current and next scheduled set point, scheduled time for next set point and gas usage today and this month.")]
    [return: Description("Information, formatted in JSON containing the current mode, living room temperature, outdoor temperature, current and next scheduled set point, scheduled time for next set point and gas usage today and this month.")]
    public async Task<string> GetHeatingInformation()
    {
        var heatingInfo = await _mediator.Send(new GetHeatingOverviewQuery());
        return JsonSerializer.Serialize(heatingInfo);
    }
}