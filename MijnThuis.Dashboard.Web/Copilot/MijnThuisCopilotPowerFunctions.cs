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
        return response.ImportToday;
    }

    [KernelFunction]
    [Description("Gets the energy use for this month in kWh.")]
    public async Task<decimal> GetEnergyUseThisMonth()
    {
        var response = await _mediator.Send(new GetPowerOverviewQuery());
        return response.ImportThisMonth;
    }

    [KernelFunction]
    [Description("Gets the price in eurocents per kWh for consuming 1kWh of energy right now. A positive number would cost me money, a negative number would make me money.")]
    public async Task<decimal> GetCurrentConsumptionPrice()
    {
        var response = await _mediator.Send(new GetPowerOverviewQuery());
        return response.CurrentConsumptionPrice;
    }

    [KernelFunction]
    [Description("Gets the price in eurocents per kWh for injecting 1kWh of energy right now. A positive number would make me money, a negative number would cost me money.")]
    public async Task<decimal> CurrentInjectionPrice()
    {
        var response = await _mediator.Send(new GetPowerOverviewQuery());
        return response.CurrentInjectionPrice;
    }
}