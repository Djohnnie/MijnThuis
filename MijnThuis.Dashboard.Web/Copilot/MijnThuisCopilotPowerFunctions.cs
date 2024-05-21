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
    [Description("Gets the current live power usage in kW.")]
    public async Task<decimal> GetPowerUsage()
    {
        var response = await _mediator.Send(new GetPowerOverviewQuery());
        return response.CurrentPower / 1000;
    }

    [KernelFunction]
    [Description("Gets the power peek for this month in kW.")]
    public async Task<decimal> GetPowerPeek()
    {
        var response = await _mediator.Send(new GetPowerOverviewQuery());
        return response.PowerPeak / 1000;
    }

    [KernelFunction]
    [Description("Gets the energy use for today in kWh.")]
    public async Task<decimal> GetEnergyUseToday()
    {
        var response = await _mediator.Send(new GetPowerOverviewQuery());
        return response.EnergyToday;
    }

    [KernelFunction]
    [Description("Gets the energy use for this month in kWh.")]
    public async Task<decimal> GetEnergyUseThisMonth()
    {
        var response = await _mediator.Send(new GetPowerOverviewQuery());
        return response.EnergyThisMonth;
    }
}