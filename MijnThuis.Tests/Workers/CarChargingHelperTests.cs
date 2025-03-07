using FakeItEasy;
using MijnThuis.DataAccess;
using MijnThuis.Integrations.Car;
using MijnThuis.Integrations.Solar;
using MijnThuis.Worker.Helpers;
using Shouldly;

namespace MijnThuis.Tests.Workers;

public class CarChargingHelperTests
{
    [Theory]
    [InlineData(false, "Unknown")]
    [InlineData(false, "Thuis")]
    [InlineData(true, "Work")]
    public async Task Given_A_Car_That_Is_Not_Ready_For_Charging_Should_Not_Charge(bool isChargePortOpen, string location)
    {
        // Mock
        var dbContext = A.Fake<MijnThuisDbContext>();
        var carService = A.Fake<ICarService>();
        var solarService = A.Fake<ISolarService>();

        A.CallTo(() => carService.GetOverview()).Returns(new CarOverview { IsChargePortOpen = isChargePortOpen });
        A.CallTo(() => carService.GetLocation()).Returns(new CarLocation { Location = location });

        // Arrange
        var state = new CarChargingHelperState();

        var sot = new CarChargingHelper(dbContext, carService, solarService);

        // Act
        await sot.Do(state, CancellationToken.None);

        // Assert
        state.CarIsReadyToCharge.ShouldBeFalse();
        A.CallTo(() => carService.GetOverview()).MustHaveHappenedOnceExactly();
        A.CallTo(() => carService.GetLocation()).MustHaveHappenedOnceExactly();
        //A.CallTo(() => logger.LogInformation(A<string>.That.Contains(CarChargingHelperLogging.NOT_READY_FOR_CHARGING))).MustHaveHappened();
    }

    [Fact]
    public async Task Test1()
    {
        // Mock
        var dbContext = A.Fake<MijnThuisDbContext>();
        var carService = A.Fake<ICarService>();
        var solarService = A.Fake<ISolarService>();

        A.CallTo(() => carService.GetOverview()).Returns(new CarOverview { IsChargePortOpen = true });
        A.CallTo(() => carService.GetLocation()).Returns(new CarLocation { Location = "Thuis" });
        A.CallTo(() => solarService.GetOverview()).Returns(new SolarOverview
        {
            CurrentSolarPower = 2,
            CurrentConsumptionPower = 1
        });

        // Arrange
        var state = new CarChargingHelperState { NumberOfSamplesToCollect = 2 };

        var sot = new CarChargingHelper(dbContext, carService, solarService);

        // Act
        await sot.Do(state, CancellationToken.None);

        // Assert
        state.CarIsReadyToCharge.ShouldBeTrue();
        state.CollectedSolarPower.ShouldHaveSingleItem();
        state.CollectedSolarPower.Contains(2);
        state.CollectedConsumedPower.ShouldHaveSingleItem();
        state.CollectedConsumedPower.Contains(1);
        A.CallTo(() => solarService.GetOverview()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Test2()
    {
        // Mock
        var dbContext = A.Fake<MijnThuisDbContext>();
        var carService = A.Fake<ICarService>();
        var solarService = A.Fake<ISolarService>();

        A.CallTo(() => carService.GetOverview()).Returns(new CarOverview { IsChargePortOpen = true });
        A.CallTo(() => carService.GetLocation()).Returns(new CarLocation { Location = "Thuis" });
        A.CallTo(() => solarService.GetOverview()).Returns(new SolarOverview
        {
            CurrentSolarPower = 2,
            CurrentConsumptionPower = 1
        });

        // Arrange
        var state = new CarChargingHelperState { NumberOfSamplesToCollect = 3 };

        var sot = new CarChargingHelper(dbContext, carService, solarService);

        // Act
        for (int i = 0; i < 2; i++)
        {
            await sot.Do(state, CancellationToken.None);
        }

        // Assert
        state.CarIsReadyToCharge.ShouldBeTrue();
        state.CollectedSolarPower.Count.ShouldBe(2);
        state.CollectedConsumedPower.Count.ShouldBe(2);
        A.CallTo(() => solarService.GetOverview()).MustHaveHappenedANumberOfTimesMatching(n => n == 2);
    }
}