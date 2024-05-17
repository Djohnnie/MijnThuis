using MediatR;
using Microsoft.SemanticKernel;
using MijnThuis.Contracts.Solar;
using System.ComponentModel;

namespace MijnThuis.Dashboard.Web.Copilot;

public class MijnThuisCopilotSolarFunctions
{
    private readonly IMediator _mediator;

    public MijnThuisCopilotSolarFunctions(IMediator mediator)
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

    [KernelFunction]
    [Description("Gets the current solar battery health in percentage?")]
    public async Task<int> GetSolarBatteryHealthState()
    {
        var response = await _mediator.Send(new GetSolarOverviewQuery());
        return response.BatteryHealth;
    }

    [KernelFunction]
    [Description("Gets the current solar energy in kWh?")]
    public async Task<decimal> GetSolarEnergy()
    {
        var response = await _mediator.Send(new GetSolarOverviewQuery());
        return response.CurrentSolarPower;
    }
}