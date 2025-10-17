using MediatR;
using Microsoft.Extensions.AI;
using MijnThuis.Contracts.Car;
using System.ComponentModel;

namespace MijnThuis.Dashboard.Web.Copilot;

public class MijnThuisCopilotCarFunctions
{
    public static IList<AITool> GetTools()
    {
        return [
            AIFunctionFactory.Create(GetCarLocation),
            AIFunctionFactory.Create(IsCarLocked),
            AIFunctionFactory.Create(IsCharging),
            AIFunctionFactory.Create(GetRange),
            AIFunctionFactory.Create(GetCarBattery),
            AIFunctionFactory.Create(GetCarBatteryHealth),
            AIFunctionFactory.Create(CarFart),
            AIFunctionFactory.Create(CarPreheat),
            AIFunctionFactory.Create(LockCar),
            AIFunctionFactory.Create(UnlockCar),
            AIFunctionFactory.Create(GetCharger1State),
            AIFunctionFactory.Create(GetCharger2State)
        ];
    }

    [Description("Gets the location where the car is parked or driving.")]
    public static async Task<string> GetCarLocation(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetCarOverviewQuery());
        return response.Address;
    }

    [Description("Is the car locked?")]
    public static async Task<bool> IsCarLocked(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetCarOverviewQuery());
        return response.IsLocked;
    }

    [Description("Is the car charging?")]
    public static async Task<bool> IsCharging(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetCarOverviewQuery());
        return response.IsCharging;
    }

    [Description("Gets the remaining range for my car in km.")]
    public static async Task<int> GetRange(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetCarOverviewQuery());
        return response.RemainingRange;
    }

    [Description("Gets the remaining car battery percentage.")]
    public static async Task<int> GetCarBattery(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetCarOverviewQuery());
        return response.BatteryLevel;
    }

    [Description("Gets the health percentage of the car battery.")]
    public static async Task<int> GetCarBatteryHealth(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetCarOverviewQuery());
        return response.BatteryHealth;
    }

    [Description("Makes the car play a fart sound.")]
    public static async Task CarFart(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new CarFartCommand());
    }

    [Description("Preheats the car.")]
    public static async Task CarPreheat(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new PreheatCarCommand());
    }

    [Description("Locks the car.")]
    public static async Task LockCar(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new LockCarCommand());
    }

    [Description("Unlocks the car.")]
    public static async Task UnlockCar(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new UnlockCarCommand());
    }

    [Description("Gets the number of available chargers for the 'Adrien Dezaegerplein' location.")]
    public static async Task<string> GetCharger1State(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetCarOverviewQuery());
        return response.Charger1;
    }

    [Description("Gets the number of available chargers for the 'Breendonkstraat' location.")]
    public static async Task<string> GetCharger2State(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetCarOverviewQuery());
        return response.Charger2;
    }
}