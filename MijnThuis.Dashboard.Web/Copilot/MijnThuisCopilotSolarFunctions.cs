using MediatR;
using Microsoft.Extensions.AI;
using MijnThuis.Contracts.Solar;
using System.ComponentModel;

namespace MijnThuis.Dashboard.Web.Copilot;

public class MijnThuisCopilotSolarFunctions
{
    public static IList<AITool> GetTools()
    {
        return [
            AIFunctionFactory.Create(GetSolarEnergyToday),
            AIFunctionFactory.Create(GetSolarEnergyThisMonth),
            AIFunctionFactory.Create(GetSolarBatteryChargeState),
            AIFunctionFactory.Create(GetSolarBatteryHealthState),
            AIFunctionFactory.Create(GetSolarPower),
            AIFunctionFactory.Create(GetBatteryPower),
            AIFunctionFactory.Create(GetGridPower),
            AIFunctionFactory.Create(GetBatteryMaximumEnergy),
            AIFunctionFactory.Create(GetSolarForecastToday),
            AIFunctionFactory.Create(GetSolarForecastTomorrow)
        ];
    }

    [Description("Gets the solar energy generated today in kWh.")]
    public static async Task<decimal> GetSolarEnergyToday(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetSolarOverviewQuery());
        return response.LastDayEnergy;
    }

    [Description("Gets the solar energy generated this month in kWh.")]
    public static async Task<decimal> GetSolarEnergyThisMonth(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetSolarOverviewQuery());
        return response.LastMonthEnergy;
    }

    [Description("Gets the current solar battery charge state in percentage. A value of 100% is fully charged.")]
    public static async Task<int> GetSolarBatteryChargeState(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetSolarOverviewQuery());
        return response.BatteryLevel;
    }

    [Description("Gets the current solar battery health in percentage. A value larger than 100% is a very good health.")]
    public static async Task<int> GetSolarBatteryHealthState(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetSolarOverviewQuery());
        return response.BatteryHealth;
    }

    [Description("Gets the current solar power in kW. A zero value is no solar power.")]
    public static async Task<decimal> GetSolarPower(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetSolarOverviewQuery());
        return response.CurrentSolarPower;
    }

    [Description("Gets the current battery power in kW. A negative value is power draining the battery. A positive value is power charging the battery.")]
    public static async Task<decimal> GetBatteryPower(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetSolarOverviewQuery());
        return response.CurrentBatteryPower;
    }

    [Description("Gets the current grid power in kW. A positive value is power returned to the grid. A negative value is power taken from the grid.")]
    public static async Task<decimal> GetGridPower(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetSolarOverviewQuery());
        return response.CurrentGridPower;
    }

    [Description("Gets the maximum energy capacity of the home battery in Watthours.")]
    [return: Description("The maximum energy capacity of the home battery in Watthours.")]
    public static async Task<int> GetBatteryMaximumEnergy(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetSolarOverviewQuery());
        return response.BatteryMaxEnergy;
    }

    [Description("Gets the solar energy forecast for today.")]
    [return: Description("The solar energy forecast for today in kWh.")]
    public static async Task<decimal> GetSolarForecastToday(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetSolarOverviewQuery());
        return response.SolarForecastToday;
    }

    [Description("Gets the solar energy forecast for tomorrow.")]
    [return: Description("The solar energy forecast for tomorrow in kWh.")]
    public static async Task<decimal> GetSolarForecastTomorrow(IServiceProvider serviceProvider)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new GetSolarOverviewQuery());
        return response.SolarForecastTomorrow;
    }
}