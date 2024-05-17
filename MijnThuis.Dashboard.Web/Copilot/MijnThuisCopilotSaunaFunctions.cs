using MediatR;
using Microsoft.SemanticKernel;
using MijnThuis.Contracts.Sauna;
using System.ComponentModel;

namespace MijnThuis.Dashboard.Web.Copilot;

public class MijnThuisCopilotSaunaFunctions
{
    private readonly IMediator _mediator;

    public MijnThuisCopilotSaunaFunctions(IMediator mediator)
    {
        _mediator = mediator;
    }

    [KernelFunction]
    [Description("Is the sauna turned on?")]
    public async Task<bool> IsSaunaOn()
    {
        var response = await _mediator.Send(new GetSaunaOverviewQuery());
        return response.State == "Sauna";
    }

    [KernelFunction]
    [Description("Is the infrared sauna turned on?")]
    public async Task<bool> IsInfraredOn()
    {
        var response = await _mediator.Send(new GetSaunaOverviewQuery());
        return response.State == "Infrarood";
    }

    [KernelFunction]
    [Description("Gets the temperature in the sauna?")]
    public async Task<int> GetSaunaTemperature()
    {
        var response = await _mediator.Send(new GetSaunaOverviewQuery());
        return response.InsideTemperature;
    }

    [KernelFunction]
    [Description("Turns on the sauna.")]
    public async Task TurnOnSauna()
    {
        await _mediator.Send(new StartSaunaCommand());
    }

    [KernelFunction]
    [Description("Turns on the infrared sauna.")]
    public async Task TurnOnInfrared()
    {
        await _mediator.Send(new StartInfraredCommand());
    }

    [KernelFunction]
    [Description("Turns of the sauna and infrared sauna.")]
    public async Task TurnOff()
    {
        await _mediator.Send(new StopSaunaCommand());
    }
}