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
    [Description("Gets the solar energy generated today in kWh.")]
    public async Task<decimal> GetSolarEnergyToday()
    {
        var response = await _mediator.Send(new GetSolarOverviewQuery());
        return response.LastDayEnergy;
    }

    [KernelFunction]
    [Description("Gets the solar energy generated this month in kWh.")]
    public async Task<decimal> GetSolarEnergyThisMonth()
    {
        var response = await _mediator.Send(new GetSolarOverviewQuery());
        return response.LastMonthEnergy;
    }

    [KernelFunction]
    [Description("Gets the current solar battery charge state in percentage. A value of 100% is fully charged.")]
    public async Task<int> GetSolarBatteryChargeState()
    {
        var response = await _mediator.Send(new GetSolarOverviewQuery());
        return response.BatteryLevel;
    }

    [KernelFunction]
    [Description("Gets the current solar battery health in percentage. A value larger than 100% is a very good health.")]
    public async Task<int> GetSolarBatteryHealthState()
    {
        var response = await _mediator.Send(new GetSolarOverviewQuery());
        return response.BatteryHealth;
    }

    [KernelFunction]
    [Description("Gets the current solar power in kW. A zero value is no solar power.")]
    public async Task<decimal> GetSolarPower()
    {
        var response = await _mediator.Send(new GetSolarOverviewQuery());
        return response.CurrentSolarPower;
    }

    [KernelFunction]
    [Description("Gets the current battery power in kW. A negative value is power draining the battery. A positive value is power charging the battery.")]
    public async Task<decimal> GetBatteryPower()
    {
        var response = await _mediator.Send(new GetSolarOverviewQuery());
        return response.CurrentBatteryPower;
    }

    [KernelFunction]
    [Description("Gets the current grid power in kW. A positive value is power returned to the grid. A negative value is power taken from the grid.")]
    public async Task<decimal> GetGridPower()
    {
        var response = await _mediator.Send(new GetSolarOverviewQuery());
        return response.CurrentGridPower;
    }

    [KernelFunction]
    [Description("Gets the maximum energy capacity of the home battery.")]
    [return: Description("The maximum energy capacity of the home battery in Wh.")]
    public async Task<int> GetBatteryMaximumEnergy()
    {
        var response = await _mediator.Send(new GetSolarOverviewQuery());
        return response.BatteryMaxEnergy;
    }

    [KernelFunction]
    [Description("Gets the solar energy forecast for today.")]
    [return: Description("The solar energy forecast for today in kWh.")]
    public async Task<decimal> GetSolarForecastToday()
    {
        var response = await _mediator.Send(new GetSolarOverviewQuery());
        return response.SolarForecastToday;
    }

    [KernelFunction]
    [Description("Gets the solar energy forecast for tomorrow.")]
    [return: Description("The solar energy forecast for tomorrow in kWh.")]
    public async Task<decimal> GetSolarForecastTomorrow()
    {
        var response = await _mediator.Send(new GetSolarOverviewQuery());
        return response.SolarForecastTomorrow;
    }
}