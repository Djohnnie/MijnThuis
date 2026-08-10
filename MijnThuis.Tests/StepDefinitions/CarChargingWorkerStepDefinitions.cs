using FakeItEasy;
using MijnThuis.DataAccess.Entities;
using MijnThuis.DataAccess.Repositories;
using MijnThuis.Integrations.Car;
using MijnThuis.Integrations.Solar;
using MijnThuis.Worker.Helpers;
using Shouldly;

namespace MijnThuis.Tests.StepDefinitions;

[Binding]
public class CarChargingWorkerStepDefinitions
{
    private IFlagRepository? _flagRepository;
    private ICarChargingEnergyHistoryRepository _carChargingEnergyHistoryRepository;
    private ICarService? _carService;
    private IModbusService? _modbusService;
    private CarChargingHelper? _sot;

    private CarChargingHelperState? _state;
    private CarOverview? _carOverview;
    private BatteryLevel? _batteryLevel;
    private ManualCarChargeFlag? _manualCarChargeFlag;

    [BeforeScenario]
    public void BeforeScenario()
    {
        _flagRepository = A.Fake<IFlagRepository>();
        _carChargingEnergyHistoryRepository = A.Fake<ICarChargingEnergyHistoryRepository>();
        _carService = A.Fake<ICarService>();
        _modbusService = A.Fake<IModbusService>();

        _sot = new CarChargingHelper(_flagRepository, _carChargingEnergyHistoryRepository, _carService, _modbusService);

        _state = new CarChargingHelperState();
        _carOverview = new CarOverview { ChargeLimit = 100 };
        _batteryLevel = new BatteryLevel();
        _manualCarChargeFlag = ManualCarChargeFlag.Default;

        A.CallTo(() => _carService!.GetOverview()).Returns(_carOverview);
        A.CallTo(() => _modbusService!.GetBatteryLevel()).Returns(_batteryLevel);
        A.CallTo(() => _flagRepository!.GetManualCarChargeFlag()).ReturnsLazily(() => _manualCarChargeFlag!);
    }

    [Given("The car charge port is not open")]
    public void GivenTheCarChargePortIsNotOpen()
    {
        _carOverview!.IsChargePortOpen = false;
    }

    [Given("The car charge port is open")]
    public void GivenTheCarChargePortIsOpen()
    {
        _carOverview!.IsChargePortOpen = true;
    }

    [Given("The car has a maximum charging speed of {int}A")]
    public void GivenTheCarHasAMaximumChargingSpeedOf(int amps)
    {
        _carOverview!.MaxChargingAmps = amps;
    }


    [Given("The car is parked at {string}")]
    public void GivenTheCarIsParkedAt(string location)
    {
        A.CallTo(() => _carService!.GetLocation()).Returns(new CarLocation { Location = location });
    }

    [Given("The home battery is charged to {int}%")]
    public void GivenTheHomeBatteryIsChargedTo(int percentage)
    {
        _batteryLevel!.Level = percentage;
    }

    [Given("the manual car charge is set to {int}A")]
    public void GivenTheManualCarChargeIsSetTo(int amps)
    {
        _manualCarChargeFlag = new ManualCarChargeFlag { ShouldCharge = true, ChargeAmps = amps };
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
        A.CallTo(() => _modbusService!.GetBatteryLevel()).MustHaveHappenedOnceExactly();
    }


    [Then("The car should not be ready to charge")]
    public void ThenTheCarShouldNotBeReadyToCharge()
    {
        _state.ShouldNotBeNull();
        _state.CarIsReadyToCharge.ShouldBeFalse();
        _state.Result.Type.ShouldBe(CarChargingHelperResultType.NotReadyForCharging);
    }

    [Then("The car should be ready to charge")]
    public void ThenTheCarShouldBeReadyToCharge()
    {
        _state.ShouldNotBeNull();
        _state.CarIsReadyToCharge.ShouldBeTrue();
    }

    [Then("The car should not be charging")]
    public void ThenTheCarShouldNotBeCharging()
    {
        _state.ShouldNotBeNull();
        _state.Result.ShouldNotBeNull();
        _state.Result.Type.ShouldBe(CarChargingHelperResultType.NotCharging);
    }

    [Then("The car should be charging")]
    public void ThenTheCarShouldBeCharging()
    {
        _state.ShouldNotBeNull();
        _state.Result.ShouldNotBeNull();
        _state.Result.Type.ShouldBe(CarChargingHelperResultType.Charging);
    }

    [Then("The car should be charging at {int}A")]
    public void ThenTheCarShouldBeChargingAt(int amps)
    {
        _state.ShouldNotBeNull();
        _state.Result.ShouldNotBeNull();
        _state.Result.Type.ShouldBe(CarChargingHelperResultType.Charging);
        _state.Result.ChargingAmps.ShouldBe(amps);
    }

    [Then("The car should have started charging")]
    public void ThenTheCarShouldHaveStartedCharging()
    {
        _state.ShouldNotBeNull();
        _state.Result.ShouldNotBeNull();
        _state.Result.Type.ShouldBe(CarChargingHelperResultType.ChargingStarted);
    }

    [Then("The car should be manually charging at {int}A")]
    public void ThenTheCarShouldBeManuallyChargingAt(int amps)
    {
        _state.ShouldNotBeNull();
        _state.Result.ShouldNotBeNull();
        _state.Result.Type.ShouldBe(CarChargingHelperResultType.ChargingManually);
        _state.Result.ChargingAmps.ShouldBe(amps);
    }

    [Then("The car should have started charging at {int}A")]
    public void ThenTheCarShouldHaveStartedChargingAt(int amps)
    {
        _state.ShouldNotBeNull();
        _state.Result.ShouldNotBeNull();
        _state.Result.Type.ShouldBe(CarChargingHelperResultType.ChargingStarted);
        _state.Result.ChargingAmps.ShouldBe(amps);
    }

}