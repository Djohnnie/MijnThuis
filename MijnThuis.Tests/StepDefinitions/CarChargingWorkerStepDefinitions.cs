using FakeItEasy;
using MijnThuis.Integrations.Car;
using MijnThuis.Integrations.Solar;
using MijnThuis.Worker.Helpers;
using Shouldly;

namespace MijnThuis.Tests.StepDefinitions;

[Binding]
public class CarChargingWorkerStepDefinitions
{
    private ICarService? _carService;
    private ISolarService? _solarService;
    private CarChargingHelper? _sot;

    private CarChargingHelperState? _state;

    [BeforeScenario]
    public void BeforeScenario()
    {
        _carService = A.Fake<ICarService>();
        _solarService = A.Fake<ISolarService>();

        _sot = new CarChargingHelper(null, _carService, _solarService);

        _state = new CarChargingHelperState();
    }

    [Given("The car charge port is not open")]
    public void GivenTheCarChargePortIsNotOpen()
    {
        A.CallTo(() => _carService!.GetOverview()).Returns(new CarOverview { IsChargePortOpen = false });
    }

    [Given("The car charge port is open")]
    public void GivenTheCarChargePortIsOpen()
    {
        A.CallTo(() => _carService!.GetOverview()).Returns(new CarOverview { IsChargePortOpen = true });
    }

    [Given("The car is parked at {string}")]
    public void GivenTheCarIsParkedAt(string location)
    {
        A.CallTo(() => _carService!.GetLocation()).Returns(new CarLocation { Location = location });
    }

    [Given("The home battery is charged to {int}%")]
    public void GivenTheHomeBatteryIsChargedTo(int percentage)
    {
        A.CallTo(() => _solarService!.GetOverview()).Returns(new SolarOverview { BatteryLevel = percentage });
    }

    [When("The worker runs a check")]
    public async Task WhenTheWorkerRunsACheck()
    {
        await _sot!.Do(_state!, CancellationToken.None);
    }

    [Then("The worker should have checked the car charge port")]
    public void ThenTheWorkerShouldHaveCheckedTheCarChargePort()
    {
        A.CallTo(() => _carService!.GetOverview()).MustHaveHappenedOnceExactly();
    }

    [Then("The worker should have checked the car location")]
    public void ThenTheWorkerShouldHaveCheckedTheCarLocation()
    {
        A.CallTo(() => _carService!.GetLocation()).MustHaveHappenedOnceExactly();
    }

    [Then("The worker should have checked the home battery")]
    public void ThenTheWorkerShouldHaveCheckedTheHomeBattery()
    {
        A.CallTo(() => _solarService!.GetOverview()).MustHaveHappenedOnceExactly();
    }

    [Then("The car should not be ready to charge")]
    public void ThenTheCarShouldNotBeReadyToCharge()
    {
        _state!.CarIsReadyToCharge.ShouldBeFalse();
        _state!.Result.Type.ShouldBe(CarChargingHelperResultType.NotReadyForCharging);
    }


}