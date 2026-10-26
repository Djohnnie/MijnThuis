using FakeItEasy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MijnThuis.DataAccess.Entities;
using MijnThuis.DataAccess.Repositories;
using MijnThuis.Integrations.Forecast;
using MijnThuis.Integrations.Solar;
using MijnThuis.Worker.Helpers;

namespace MijnThuis.Tests.Solar;

public class HomeBatteryChargingHelperTests
{
    private readonly IModbusService _modbusService = A.Fake<IModbusService>();
    private readonly IFlagRepository _flagRepository = A.Fake<IFlagRepository>();
    private readonly IDayAheadEnergyPricesRepository _pricesRepository = A.Fake<IDayAheadEnergyPricesRepository>();

    public HomeBatteryChargingHelperTests()
    {
        A.CallTo(() => _pricesRepository.GetCheapestEnergyPriceForTimestamp(A<DateTime>._))
            .Returns(new DayAheadCheapestEnergyPricesEntry { To = DateTime.Now.AddMinutes(15) });
    }

    [Theory]
    [InlineData(100, false)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public async Task FullBatteryCancelsManualChargingAndDoesNotRestartAt99Percent(int batteryLevel, bool scheduledCharging)
    {
        var savedFlag = new ManualHomeBatteryChargeFlag
        {
            ShouldCharge = true,
            ChargeWattage = 2000,
            ChargeUntil = DateTime.Now.AddHours(8)
        };
        A.CallTo(() => _flagRepository.GetManualHomeBatteryChargeFlag())
            .ReturnsLazily(() => ManualHomeBatteryChargeFlag.Deserialize(savedFlag.Serialize()));
        A.CallTo(() => _flagRepository.SetManualHomeBatteryChargeFlag(false, 0, DateTime.MinValue))
            .Invokes(() => savedFlag = ManualHomeBatteryChargeFlag.Default);
        A.CallTo(() => _pricesRepository.GetCheapestEnergyPriceForTimestamp(A<DateTime>._))
            .Returns(new DayAheadCheapestEnergyPricesEntry
            {
                To = DateTime.Now.AddMinutes(15),
                ShouldCharge = scheduledCharging
            });
        SetOverview(batteryLevel);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var helper = CreateHelper(cache);

        await helper.CheckForBatteryCharging(CancellationToken.None);

        A.CallTo(() => _flagRepository.SetManualHomeBatteryChargeFlag(false, 0, DateTime.MinValue))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _modbusService.StopChargingBattery()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _modbusService.StartChargingBattery(A<TimeSpan>._, A<int>._)).MustNotHaveHappened();

        SetOverview(99, StorageControlMode.MaximizeSelfConsumption);
        await helper.CheckForBatteryCharging(CancellationToken.None);

        A.CallTo(() => _flagRepository.SetManualHomeBatteryChargeFlag(false, 0, DateTime.MinValue))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _modbusService.StartChargingBattery(A<TimeSpan>._, A<int>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ManualChargingContinuesBelowFullBeforeExpiry()
    {
        A.CallTo(() => _flagRepository.GetManualHomeBatteryChargeFlag())
            .Returns(new ManualHomeBatteryChargeFlag
            {
                ShouldCharge = true,
                ChargeWattage = 2000,
                ChargeUntil = DateTime.Now.AddHours(8)
            });
        SetOverview(99);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var helper = CreateHelper(cache);

        await helper.CheckForBatteryCharging(CancellationToken.None);

        A.CallTo(() => _flagRepository.SetManualHomeBatteryChargeFlag(false, 0, DateTime.MinValue))
            .MustNotHaveHappened();
        A.CallTo(() => _modbusService.StartChargingBattery(A<TimeSpan>._, 2000)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _modbusService.StopChargingBattery()).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExpiredManualChargingStopsInTheSameCheck()
    {
        A.CallTo(() => _flagRepository.GetManualHomeBatteryChargeFlag())
            .Returns(new ManualHomeBatteryChargeFlag
            {
                ShouldCharge = true,
                ChargeWattage = 2000,
                ChargeUntil = DateTime.Now.AddMinutes(-1)
            });
        SetOverview(80);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var helper = CreateHelper(cache);

        await helper.CheckForBatteryCharging(CancellationToken.None);

        A.CallTo(() => _flagRepository.SetManualHomeBatteryChargeFlag(false, 0, DateTime.MinValue))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _modbusService.StopChargingBattery()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _modbusService.StartChargingBattery(A<TimeSpan>._, A<int>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task StalledManualChargingStopsAfterConsecutiveIterationsBelowFull()
    {
        var savedFlag = new ManualHomeBatteryChargeFlag
        {
            ShouldCharge = true,
            ChargeWattage = 2000,
            ChargeUntil = DateTime.Now.AddHours(8)
        };
        A.CallTo(() => _flagRepository.GetManualHomeBatteryChargeFlag())
            .ReturnsLazily(() => ManualHomeBatteryChargeFlag.Deserialize(savedFlag.Serialize()));
        A.CallTo(() => _flagRepository.SetManualHomeBatteryChargeFlag(false, 0, DateTime.MinValue))
            .Invokes(() => savedFlag = ManualHomeBatteryChargeFlag.Default);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var helper = CreateHelper(cache);

        SetOverview(89, batteryPower: 0);
        await helper.CheckForBatteryCharging(CancellationToken.None);
        await helper.CheckForBatteryCharging(CancellationToken.None);

        A.CallTo(() => _modbusService.StopChargingBattery()).MustNotHaveHappened();

        await helper.CheckForBatteryCharging(CancellationToken.None);

        A.CallTo(() => _flagRepository.SetManualHomeBatteryChargeFlag(false, 0, DateTime.MinValue))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _modbusService.StopChargingBattery()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ActiveBatteryChargingResetsStalledCount()
    {
        A.CallTo(() => _flagRepository.GetManualHomeBatteryChargeFlag())
            .Returns(new ManualHomeBatteryChargeFlag
            {
                ShouldCharge = true,
                ChargeWattage = 2000,
                ChargeUntil = DateTime.Now.AddHours(8)
            });
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var helper = CreateHelper(cache);

        SetOverview(80, batteryPower: 0);
        await helper.CheckForBatteryCharging(CancellationToken.None);
        await helper.CheckForBatteryCharging(CancellationToken.None);
        SetOverview(80, batteryPower: 1500);
        await helper.CheckForBatteryCharging(CancellationToken.None);
        SetOverview(80, batteryPower: 0);
        await helper.CheckForBatteryCharging(CancellationToken.None);
        await helper.CheckForBatteryCharging(CancellationToken.None);

        A.CallTo(() => _flagRepository.SetManualHomeBatteryChargeFlag(false, 0, DateTime.MinValue))
            .MustNotHaveHappened();
        A.CallTo(() => _modbusService.StopChargingBattery()).MustNotHaveHappened();
    }

    [Fact]
    public async Task HighConsumptionDoesNotCountAsStalledCharging()
    {
        A.CallTo(() => _flagRepository.GetManualHomeBatteryChargeFlag())
            .Returns(new ManualHomeBatteryChargeFlag
            {
                ShouldCharge = true,
                ChargeWattage = 2000,
                ChargeUntil = DateTime.Now.AddHours(8)
            });
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var helper = CreateHelper(cache);

        SetOverview(80, batteryPower: 0, consumptionPower: 2500);
        for (var i = 0; i < 5; i++)
        {
            await helper.CheckForBatteryCharging(CancellationToken.None);
        }

        A.CallTo(() => _flagRepository.SetManualHomeBatteryChargeFlag(false, 0, DateTime.MinValue))
            .MustNotHaveHappened();
    }

    private void SetOverview(int batteryLevel, StorageControlMode mode = StorageControlMode.RemoteControl, decimal batteryPower = 0, decimal consumptionPower = 0)
    {
        A.CallTo(() => _modbusService.GetBulkOverview()).Returns(new ModbusDataSet
        {
            BatteryLevel = batteryLevel,
            StorageControlMode = mode,
            CurrentBatteryPower = batteryPower,
            CurrentConsumptionPower = consumptionPower
        });
    }

    private HomeBatteryChargingHelper CreateHelper(IMemoryCache cache)
    {
        return new HomeBatteryChargingHelper(
            A.Fake<IForecastService>(),
            _modbusService,
            new ConfigurationBuilder().Build(),
            cache,
            _pricesRepository,
            A.Fake<ISolarPowerHistoryRepository>(),
            A.Fake<IEnergyForecastsRepository>(),
            _flagRepository,
            NullLogger<HomeBatteryChargingHelper>.Instance);
    }
}
