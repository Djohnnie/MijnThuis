using MediatR;
using Microsoft.SemanticKernel;
using MijnThuis.Contracts.Car;
using System.ComponentModel;

namespace MijnThuis.Dashboard.Web.Copilot;

public class MijnThuisCopilotCarFunctions
{
    private readonly IMediator _mediator;

    public MijnThuisCopilotCarFunctions(IMediator mediator)
    {
        _mediator = mediator;
    }

    [KernelFunction]
    [Description("Gets the location of the car.")]
    public async Task<string> GetPowerUsage()
    {
        var response = await _mediator.Send(new GetCarOverviewQuery());
        return response.Address;
    }

    [KernelFunction]
    [Description("Is the car locked?")]
    public async Task<bool> IsCarLocked()
    {
        var response = await _mediator.Send(new GetCarOverviewQuery());
        return response.IsLocked;
    }

    [KernelFunction]
    [Description("Is the car charging?")]
    public async Task<bool> IsCharging()
    {
        var response = await _mediator.Send(new GetCarOverviewQuery());
        return response.IsCharging;
    }

    [KernelFunction]
    [Description("Gets the remaining range for my car in km.")]
    public async Task<int> GetRange()
    {
        var response = await _mediator.Send(new GetCarOverviewQuery());
        return response.RemainingRange;
    }

    [KernelFunction]
    [Description("Gets the remaining car battery percentage.")]
    public async Task<int> GetCarBattery()
    {
        var response = await _mediator.Send(new GetCarOverviewQuery());
        return response.BatteryLevel;
    }

    [KernelFunction]
    [Description("Gets the health percentage of the car battery.")]
    public async Task<int> GetCarBatteryHealth()
    {
        var response = await _mediator.Send(new GetCarOverviewQuery());
        return response.BatteryHealth;
    }

    [KernelFunction]
    [Description("Makes the car play a fart sound.")]
    public async Task CarFart()
    {
        await _mediator.Send(new CarFartCommand());
    }

    [KernelFunction]
    [Description("Preheats the car.")]
    public async Task CarPreheat()
    {
        await _mediator.Send(new PreheatCarCommand());
    }

    [KernelFunction]
    [Description("Locks the car.")]
    public async Task LockCar()
    {
        await _mediator.Send(new LockCarCommand());
    }

    [KernelFunction]
    [Description("Unlocks the car.")]
    public async Task UnlockCar()
    {
        await _mediator.Send(new UnlockCarCommand());
    }

    [KernelFunction]
    [Description("Gets the number of available chargers for the 'Adrien Dezaegerplein' location.")]
    public async Task<string> GetCharger1State()
    {
        var response = await _mediator.Send(new GetCarOverviewQuery());
        return response.Charger1;
    }

    [KernelFunction]
    [Description("Gets the number of available chargers for the 'Breendonkstraat' location.")]
    public async Task<string> GetCharger2State()
    {
        var response = await _mediator.Send(new GetCarOverviewQuery());
        return response.Charger2;
    }
}