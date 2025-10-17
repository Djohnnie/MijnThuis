using MediatR;
using Microsoft.Extensions.AI;
using MijnThuis.Contracts.Power;
using System.ComponentModel;

namespace MijnThuis.Dashboard.Web.Copilot;

public class MijnThuisCopilotPowerFunctions
{
    public static IList<AITool> GetTools()
    {
        return [
            AIFunctionFactory.Create(GetPowerUsage),
            AIFunctionFactory.Create(GetPowerPeek),
            AIFunctionFactory.Create(GetEnergyUseToday),
            AIFunctionFactory.Create(GetEnergyUseThisMonth),
            AIFunctionFactory.Create(GetCurrentConsumptionPrice),
            AIFunctionFactory.Create(CurrentInjectionPrice)
        ];
    }

    [Description("Gets the current live power usage in kW.")]
    public static async Task<decimal> GetPowerUsage(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetPowerOverviewQuery());
        return response.CurrentPower / 1000;
    }

    [Description("Gets the power peek for this month in kW.")]
    public static async Task<decimal> GetPowerPeek(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetPowerOverviewQuery());
        return response.PowerPeak / 1000;
    }

    [Description("Gets the energy use for today in kWh.")]
    public static async Task<decimal> GetEnergyUseToday(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetPowerOverviewQuery());
        return response.ImportToday;
    }

    [Description("Gets the energy use for this month in kWh.")]
    public static async Task<decimal> GetEnergyUseThisMonth(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetPowerOverviewQuery());
        return response.ImportThisMonth;
    }

    [Description("Gets the price in eurocents per kWh for consuming 1kWh of energy right now. A positive number would cost me money, a negative number would make me money.")]
    public static async Task<decimal> GetCurrentConsumptionPrice(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetPowerOverviewQuery());
        return response.CurrentConsumptionPrice;
    }

    [Description("Gets the price in eurocents per kWh for injecting 1kWh of energy right now. A positive number would make me money, a negative number would cost me money.")]
    public static async Task<decimal> CurrentInjectionPrice(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetPowerOverviewQuery());
        return response.CurrentInjectionPrice;
    }
}