using MediatR;
using Microsoft.SemanticKernel;
using MijnThuis.Contracts.Solar;
using System.ComponentModel;

namespace MijnThuis.Dashboard.Web.Copilot;

public class MijnThuisCopilotFunctions
{
    private readonly IMediator _mediator;

    public MijnThuisCopilotFunctions(IMediator mediator)
    {
        _mediator = mediator;
    }

    [KernelFunction]
    [Description("Gets the current solar battery charge state in percentage?")]
    public async Task<int> GetSolarBatteryChargeState()
    {
        var response = await _mediator.Send(new GetSolarOverviewQuery());
        return response.BatteryLevel;
    }
}