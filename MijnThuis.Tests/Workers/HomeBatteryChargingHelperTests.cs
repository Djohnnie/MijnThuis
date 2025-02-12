using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MijnThuis.Integrations.Forecast;
using MijnThuis.Integrations.Power;
using MijnThuis.Integrations.Solar;
using MijnThuis.Worker.Helpers;
using Shouldly;

namespace MijnThuis.Tests.Workers;

public class HomeBatteryChargingHelperTests
{
    [Fact]
    public async Task When_Battery_Is_Charging_But_Home_Usage_Is_Above_Threshold_Then_Battery_Should_Stop_Charging()
    {
        //Arrange
        Dictionary<string, string> settings = new()
        {
            ["START_TIME_IN_HOURS"] = "0",
            ["END_TIME_IN_HOURS"] = "23",
            ["GRID_CHARGING_POWER"] = "1700",
            ["GRID_CHARGING_THRESHOLD"] = "2500",
            ["BATTERY_LEVEL_THRESHOLD"] = "100",
            ["STANDBY_USAGE"] = "250"
        };
        var dateTime = DateTime.Now;

        var powerOverview = new PowerOverview
        {
            CurrentPower = 5000
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        // Mock
        var forecastService = A.Fake<IForecastService>();
        var modbusService = A.Fake<IModbusService>();
        var powerService = A.Fake<IPowerService>();

        var logger = A.Fake<ILogger<HomeBatteryChargingHelper>>();

        A.CallTo(() => powerService.GetOverview()).Returns(powerOverview);

        // Arrange
        var sot = new HomeBatteryChargingHelper(forecastService, modbusService, powerService, configuration, logger);

        // Act
        var result = await sot.Verify(new BatteryCharged(dateTime, dateTime), CancellationToken.None);

        // Assert
        result.Charged.ShouldNotBeNull();
    }
}