using MediatR;
using Microsoft.SemanticKernel;
using MijnThuis.Contracts.Heating;
using System.ComponentModel;

namespace MijnThuis.Dashboard.Web.Copilot;

public class MijnThuisCopilotHeatingFunctions
{
    private readonly IMediator _mediator;

    public MijnThuisCopilotHeatingFunctions(IMediator mediator)
    {
        _mediator = mediator;
    }

    [KernelFunction]
    [Description("Gets the heating mode.")]
    public async Task<string> GetHeatingMode()
    {
        var response = await _mediator.Send(new GetHeatingOverviewQuery());
        return response.Mode;
    }

    [KernelFunction]
    [Description("Gets the current temperature inside the house in degrees Celcius.")]
    public async Task<decimal> GetInsideTemperature()
    {
        var response = await _mediator.Send(new GetHeatingOverviewQuery());
        return response.RoomTemperature;
    }

    [KernelFunction]
    [Description("Gets the current outside temperature in degrees Celcius.")]
    public async Task<decimal> GetOutdoorTemperature()
    {
        var response = await _mediator.Send(new GetHeatingOverviewQuery());
        return response.OutdoorTemperature;
    }

    [KernelFunction]
    [Description("Gets the next heating schedule setting.")]
    public async Task<string> GetsNextSetPoint()
    {
        var response = await _mediator.Send(new GetHeatingOverviewQuery());
        return $"{response.NextSetpoint}°C at {response.NextSwitchTime}";
    }

    [KernelFunction]
    [Description("Sets the heating to the default schedule.")]
    public async Task SetScheduledHeating()
    {
        await _mediator.Send(new SetScheduledHeatingCommand());
    }

    [KernelFunction]
    [Description("Turns off the heating.")]
    public async Task SetHeatingOff()
    {
        await _mediator.Send(new SetAntiFrostHeatingCommand());
    }
}