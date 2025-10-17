using MediatR;
using Microsoft.Extensions.AI;
using MijnThuis.Contracts.Sauna;
using System.ComponentModel;

namespace MijnThuis.Dashboard.Web.Copilot;

public class MijnThuisCopilotSaunaFunctions
{
    public static IList<AITool> GetTools()
    {
        return [
            AIFunctionFactory.Create(IsSaunaOn),
            AIFunctionFactory.Create(IsInfraredOn),
            AIFunctionFactory.Create(GetSaunaTemperature),
            AIFunctionFactory.Create(TurnOnSauna),
            AIFunctionFactory.Create(TurnOnInfrared),
            AIFunctionFactory.Create(TurnOff)
        ];
    }

    [Description("Is the sauna turned on?")]
    public static async Task<bool> IsSaunaOn(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetSaunaOverviewQuery());
        return response.State == "Sauna";
    }

    [Description("Is the infrared sauna turned on?")]
    public static async Task<bool> IsInfraredOn(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetSaunaOverviewQuery());
        return response.State == "Infrarood";
    }

    [Description("Gets the temperature in the sauna?")]
    public static async Task<int> GetSaunaTemperature(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetSaunaOverviewQuery());
        return response.InsideTemperature;
    }

    [Description("Turns on the sauna.")]
    public static async Task TurnOnSauna(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new StartSaunaCommand());
    }

    [Description("Turns on the infrared sauna.")]
    public static async Task TurnOnInfrared(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new StartInfraredCommand());
    }

    [Description("Turns of the sauna and infrared sauna.")]
    public static async Task TurnOff(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new StopSaunaCommand());
    }
}