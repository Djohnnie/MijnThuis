using MediatR;
using Microsoft.SemanticKernel;
using MijnThuis.Contracts.Power;
using System.ComponentModel;

namespace MijnThuis.Dashboard.Web.Copilot;

public class MijnThuisCopilotPowerFunctions
{
    private readonly IMediator _mediator;

    public MijnThuisCopilotPowerFunctions(IMediator mediator)
    {
        _mediator = mediator;
    }

    [KernelFunction]
    [Description("Gets the current power usage?")]
    public async Task<decimal> GetPowerUsage()
    {
        var response = await _mediator.Send(new GetPowerOverviewQuery());
        return response.CurrentPower;
    }
}