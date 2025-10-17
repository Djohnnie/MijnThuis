using MediatR;
using Microsoft.Extensions.AI;
using MijnThuis.Contracts.Heating;
using System.ComponentModel;

namespace MijnThuis.Dashboard.Web.Copilot;

public class MijnThuisCopilotHeatingFunctions
{
    public static IList<AITool> GetTools()
    {
        return [
            AIFunctionFactory.Create(GetHeatingMode),
            AIFunctionFactory.Create(GetInsideTemperature),
            AIFunctionFactory.Create(GetOutdoorTemperature),
            AIFunctionFactory.Create(GetsNextSetPoint),
            AIFunctionFactory.Create(SetScheduledHeating),
            AIFunctionFactory.Create(SetHeatingOff)
        ];
    }

    [Description("Gets the heating mode.")]
    public static async Task<string> GetHeatingMode(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetHeatingOverviewQuery());
        return response.Mode;
    }

    [Description("Gets the current temperature inside the house in degrees Celcius.")]
    public static async Task<decimal> GetInsideTemperature(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetHeatingOverviewQuery());
        return response.RoomTemperature;
    }

    [Description("Gets the current outside temperature in degrees Celcius.")]
    public static async Task<decimal> GetOutdoorTemperature(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetHeatingOverviewQuery());
        return response.OutdoorTemperature;
    }

    [Description("Gets the next heating schedule setting.")]
    public static async Task<string> GetsNextSetPoint(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetHeatingOverviewQuery());
        return $"{response.NextSetpoint}°C at {response.NextSwitchTime}";
    }

    [Description("Sets the heating to the default schedule.")]
    public static async Task SetScheduledHeating(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new SetScheduledHeatingCommand());
    }

    [Description("Turns off the heating.")]
    public static async Task SetHeatingOff(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new SetAntiFrostHeatingCommand());
    }
}