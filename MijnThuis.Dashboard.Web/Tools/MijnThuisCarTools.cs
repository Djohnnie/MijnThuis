using MediatR;
using MijnThuis.Contracts.Car;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace MijnThuis.Dashboard.Web.Tools;

[McpServerToolType]
public class MijnThuisCarTools
{
    private readonly IMediator _mediator;

    public MijnThuisCarTools(IMediator mediator)
    {
        _mediator = mediator;
    }

    [McpServerTool(Name = $"mijnthuis_car_{nameof(GetCarInformation)}", ReadOnly = true)]
    [Description("Gets information about my electric car like location, battery level and health, charging statistics, temperature and charging possibilities.")]
    [return: Description("Information, formatted in JSON containing the car location, battery level and health, charging statistics, temperature and charging possibilities.")]
    public async Task<string> GetCarInformation()
    {
        var carInfo = await _mediator.Send(new GetCarOverviewQuery());
        return JsonSerializer.Serialize(carInfo);
    }

    [McpServerTool(Name = $"mijnthuis_car_{nameof(UnlockCar)}", Idempotent = true)]
    [Description("Unlocks my electric car.")]
    [return: Description("True, if the car unlocked successfully. False, if the car did not unlock successfully or if the provided pin code was invalid.")]
    public async Task<bool> UnlockCar([Description("The pin code needed to be able to execute sensitive commands.")] string pin)
    {
        var response = await _mediator.Send(new UnlockCarCommand { Pin = pin });
        return response.Success;
    }

    [McpServerTool(Name = $"mijnthuis_car_{nameof(LockCar)}", Idempotent = true)]
    [Description("Locks my electric car.")]
    [return: Description("True, if the car locked successfully. False, if the car did not lock successfully or if the provided pin code was invalid.")]
    public async Task<bool> LockCar([Description("The pin code needed to be able to execute sensitive commands.")] string pin)
    {
        var response = await _mediator.Send(new LockCarCommand { Pin = pin });
        return response.Success;
    }

    [McpServerTool(Name = $"mijnthuis_car_{nameof(StartChargingCar)}", Idempotent = true)]
    [Description("Start charging my electric car.")]
    [return: Description("True, if the car started charging successfully. False, otherwise.")]
    public async Task<bool> StartChargingCar(
        [Description("The number of amps to charge the car with.")] int chargeAmps,
        [Description("The pin code needed to be able to execute sensitive commands.")] string pin)
    {
        var response = await _mediator.Send(new SetManualCarChargeCommand
        {
            IsEnabled = true,
            ChargeAmps = chargeAmps,
            Pin = pin
        });

        return response.Success;
    }

    [McpServerTool(Name = $"mijnthuis_car_{nameof(StopChargingCar)}", Idempotent = true)]
    [Description("Stop charging my electric car.")]
    [return: Description("True, if the car stopped charging successfully. False, otherwise.")]
    public async Task<bool> StopChargingCar(
        [Description("The pin code needed to be able to execute sensitive commands.")] string pin)
    {
        var response = await _mediator.Send(new SetManualCarChargeCommand
        {
            IsEnabled = true,
            ChargeAmps = 16,
            Pin = pin
        });

        return response.Success;
    }
}