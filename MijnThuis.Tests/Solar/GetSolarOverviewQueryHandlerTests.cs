using FakeItEasy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using MijnThuis.Application.Solar.Queries;
using MijnThuis.Contracts.Solar;
using MijnThuis.Integrations.Forecast;
using MijnThuis.Integrations.Solar;
using Shouldly;
using System.Net;
using Xunit;

namespace MijnThuis.Tests.Solar;

public class GetSolarOverviewQueryHandlerTests
{
    private readonly ISolarService _solarService = A.Fake<ISolarService>();
    private readonly IForecastService _forecastService = A.Fake<IForecastService>();
    private readonly IModbusService _modbusService = A.Fake<IModbusService>();

    public GetSolarOverviewQueryHandlerTests()
    {
        A.CallTo(() => _modbusService.GetOverview()).Returns(new SolarOverview
        {
            CurrentSolarPower = 2500M,
            CurrentBatteryPower = 500M,
            CurrentGridPower = 1000M,
            CurrentConsumptionPower = 3000M,
            HasExportLimitation = true
        });
        A.CallTo(() => _modbusService.GetBatteryLevel()).Returns(new BatteryLevel
        {
            Level = 75M,
            Health = 95M,
            MaxEnergy = 10M
        });
        A.CallTo(() => _forecastService.GetSolarForecastEstimate(
            A<decimal>._, A<decimal>._, A<decimal>._, A<decimal>._, A<decimal>._, A<byte>._))
            .Returns(new ForecastOverview
            {
                EstimatedWattHoursToday = 1000,
                EstimatedWattHoursTomorrow = 2000
            });
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task EnergyRequestFailureDoesNotPreventLiveDataAndForecastsLoading(HttpStatusCode statusCode)
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(cache);
        A.CallTo(() => _solarService.GetEnergy())
            .ThrowsAsync(new HttpRequestException("SolarEdge request failed", null, statusCode));

        var response = await handler.Handle(new GetSolarOverviewQuery(), CancellationToken.None);

        response.CurrentSolarPower.ShouldBe(2.5M);
        response.CurrentBatteryPower.ShouldBe(0.5M);
        response.CurrentGridPower.ShouldBe(-1M);
        response.CurrentConsumptionPower.ShouldBe(3M);
        response.BatteryLevel.ShouldBe(75);
        response.BatteryHealth.ShouldBe(95);
        response.BatteryMaxEnergy.ShouldBe(10);
        response.HasExportLimitation.ShouldBeTrue();
        response.SolarForecastToday.ShouldBe(3M);
        response.SolarForecastTomorrow.ShouldBe(6M);
        response.LastDayEnergy.ShouldBeNull();
        response.LastMonthEnergy.ShouldBeNull();

        await handler.Handle(new GetSolarOverviewQuery(), CancellationToken.None);
        A.CallTo(() => _solarService.GetEnergy()).MustHaveHappenedOnceExactly();

        cache.Remove("SOLAR_ENERGY");
        A.CallTo(() => _solarService.GetEnergy()).Returns(new EnergyProduced
        {
            LastDayEnergy = 5000M,
            LastMonthEnergy = 100000M
        });

        var recovered = await handler.Handle(new GetSolarOverviewQuery(), CancellationToken.None);
        recovered.LastDayEnergy.ShouldBe(5M);
        recovered.LastMonthEnergy.ShouldBe(100M);
    }

    [Fact]
    public async Task SuccessfulEnergyTotalsAreConvertedAndCached()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = CreateHandler(cache);
        A.CallTo(() => _solarService.GetEnergy()).Returns(new EnergyProduced
        {
            LastDayEnergy = 0M,
            LastMonthEnergy = 100000M
        });

        var response = await handler.Handle(new GetSolarOverviewQuery(), CancellationToken.None);
        await handler.Handle(new GetSolarOverviewQuery(), CancellationToken.None);

        response.LastDayEnergy.ShouldBe(0M);
        response.LastMonthEnergy.ShouldBe(100M);
        A.CallTo(() => _solarService.GetEnergy()).MustHaveHappenedOnceExactly();
    }

    private GetSolarOverviewQueryHandler CreateHandler(IMemoryCache cache)
    {
        return new GetSolarOverviewQueryHandler(
            _solarService, _forecastService, _modbusService, cache,
            NullLogger<GetSolarOverviewQueryHandler>.Instance);
    }
}
