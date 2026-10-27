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
    private readonly ISolarPowerHistoryRepository _solarPowerHistoryRepository = A.Fake<ISolarPowerHistoryRepository>();

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

    [Fact]
    public async Task ManualChargingWithoutPricePeriodUsesChargeUntilAsDuration()
    {
        A.CallTo(() => _flagRepository.GetManualHomeBatteryChargeFlag())
            .Returns(new ManualHomeBatteryChargeFlag
            {
                ShouldCharge = true,
                ChargeWattage = 2000,
                ChargeUntil = DateTime.Now.AddHours(2)
            });
        A.CallTo(() => _pricesRepository.GetCheapestEnergyPriceForTimestamp(A<DateTime>._))
            .Returns(new DayAheadCheapestEnergyPricesEntry());
        SetOverview(80);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var helper = CreateHelper(cache);

        await helper.CheckForBatteryCharging(CancellationToken.None);

        A.CallTo(() => _modbusService.StartChargingBattery(
                A<TimeSpan>.That.Matches(d => d > TimeSpan.FromMinutes(115) && d <= TimeSpan.FromHours(2)), 2000))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _modbusService.StopChargingBattery()).MustNotHaveHappened();
    }

    [Fact]
    public async Task ManualChargingDurationIsCappedAtEightHours()
    {
        A.CallTo(() => _flagRepository.GetManualHomeBatteryChargeFlag())
            .Returns(new ManualHomeBatteryChargeFlag
            {
                ShouldCharge = true,
                ChargeWattage = 2000,
                ChargeUntil = DateTime.Now.AddHours(20)
            });
        SetOverview(80);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var helper = CreateHelper(cache);

        await helper.CheckForBatteryCharging(CancellationToken.None);

        A.CallTo(() => _modbusService.StartChargingBattery(TimeSpan.FromHours(8), 2000)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ScheduledChargingWithoutPricePeriodDoesNotStart()
    {
        A.CallTo(() => _flagRepository.GetManualHomeBatteryChargeFlag())
            .Returns(ManualHomeBatteryChargeFlag.Default);
        A.CallTo(() => _pricesRepository.GetCheapestEnergyPriceForTimestamp(A<DateTime>._))
            .Returns(new DayAheadCheapestEnergyPricesEntry { ShouldCharge = true });
        SetOverview(80, StorageControlMode.MaximizeSelfConsumption);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var helper = CreateHelper(cache);

        await helper.CheckForBatteryCharging(CancellationToken.None);

        A.CallTo(() => _modbusService.StartChargingBattery(A<TimeSpan>._, A<int>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ScheduledChargingWithPricePeriodAlmostOverDoesNotStart()
    {
        A.CallTo(() => _flagRepository.GetManualHomeBatteryChargeFlag())
            .Returns(ManualHomeBatteryChargeFlag.Default);
        A.CallTo(() => _pricesRepository.GetCheapestEnergyPriceForTimestamp(A<DateTime>._))
            .Returns(new DayAheadCheapestEnergyPricesEntry { ShouldCharge = true, To = DateTime.Now.AddSeconds(20) });
        SetOverview(80, StorageControlMode.MaximizeSelfConsumption);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var helper = CreateHelper(cache);

        await helper.CheckForBatteryCharging(CancellationToken.None);

        A.CallTo(() => _modbusService.StartChargingBattery(A<TimeSpan>._, A<int>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task PrepareCheapestPeriodsFillsInMissingPeriodsOnly()
    {
        var today = DateTime.Today;
        var prices = Enumerable.Range(0, 4)
            .Select(i => new DayAheadEnergyPricesEntry
            {
                From = today.AddMinutes(i * 15),
                To = today.AddMinutes((i + 1) * 15).AddSeconds(-1),
                EuroPerMWh = 100 - i * 10
            })
            .ToList();
        A.CallTo(() => _pricesRepository.GetEnergyPriceForDate(today, A<CancellationToken>._)).Returns(prices);
        A.CallTo(() => _pricesRepository.GetEnergyPriceForDate(today.AddDays(1), A<CancellationToken>._))
            .Returns(new List<DayAheadEnergyPricesEntry>());
        A.CallTo(() => _pricesRepository.GetCheapestEnergyPriceForDate(today, A<CancellationToken>._))
            .Returns(new List<DayAheadCheapestEnergyPricesEntry>
            {
                new() { From = prices[0].From, To = prices[0].To },
                new() { From = prices[1].From, To = prices[1].To }
            });
        var helper = CreateHelper(new MemoryCache(new MemoryCacheOptions()));

        await helper.PrepareCheapestPeriods(CancellationToken.None);

        A.CallTo(() => _pricesRepository.AddCheapestEnergyPrices(
                A<IEnumerable<DayAheadCheapestEnergyPricesEntry>>.That.Matches(x =>
                    x.Count() == 2 &&
                    x.Any(p => p.From == prices[2].From && p.Order == 2) &&
                    x.Any(p => p.From == prices[3].From && p.Order == 1)),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task PrepareCheapestPeriodsDoesNothingWhenAllPeriodsExist()
    {
        var today = DateTime.Today;
        var price = new DayAheadEnergyPricesEntry { From = today, To = today.AddMinutes(15).AddSeconds(-1), EuroPerMWh = 100 };
        A.CallTo(() => _pricesRepository.GetEnergyPriceForDate(A<DateTime>._, A<CancellationToken>._))
            .Returns(new List<DayAheadEnergyPricesEntry> { price });
        A.CallTo(() => _pricesRepository.GetCheapestEnergyPriceForDate(A<DateTime>._, A<CancellationToken>._))
            .Returns(new List<DayAheadCheapestEnergyPricesEntry> { new() { From = price.From, To = price.To } });
        var helper = CreateHelper(new MemoryCache(new MemoryCacheOptions()));

        await helper.PrepareCheapestPeriods(CancellationToken.None);

        A.CallTo(() => _pricesRepository.AddCheapestEnergyPrices(A<IEnumerable<DayAheadCheapestEnergyPricesEntry>>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ScheduleChargesCheapestPeriodWhenBatteryWouldDropBelowMinimum()
    {
        SetupSchedule(batteryLevel: 12, consumptionPerQuarter: 500);
        var cheapest = new DayAheadCheapestEnergyPricesEntry { Id = Guid.NewGuid() };
        A.CallTo(() => _pricesRepository.GetCheapestEnergyPriceUpToTimestamp(A<DateTime>._)).Returns(cheapest);
        var helper = CreateHelper(new MemoryCache(new MemoryCacheOptions()));

        await helper.UpdateChargingSchedule(CancellationToken.None);

        A.CallTo(() => _pricesRepository.SetEnergyPriceShouldCharge(cheapest.Id, true)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _pricesRepository.ClearShouldChargeFlags(A<DateTime>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ScheduleUnschedulesOnlyMostExpensivePeriodWhenBatteryStaysWellAboveMinimum()
    {
        SetupSchedule(batteryLevel: 80, consumptionPerQuarter: 0);
        var mostExpensive = new DayAheadCheapestEnergyPricesEntry { Id = Guid.NewGuid(), ShouldCharge = true };
        A.CallTo(() => _pricesRepository.GetMostExpensiveEnergyPriceUpToTimestamp(A<DateTime>._)).Returns(mostExpensive);
        var helper = CreateHelper(new MemoryCache(new MemoryCacheOptions()));

        await helper.UpdateChargingSchedule(CancellationToken.None);

        A.CallTo(() => _pricesRepository.SetEnergyPriceShouldCharge(mostExpensive.Id, false)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _pricesRepository.SetEnergyPriceShouldCharge(A<Guid>._, true)).MustNotHaveHappened();
        A.CallTo(() => _pricesRepository.ClearShouldChargeFlags(A<DateTime>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ScheduleLeavesFlagsUntouchedWhenBatteryIsJustAboveMinimum()
    {
        // 10kWh battery at 2000W grid charging: one period adds 5%, so 12% is within [10%, 15%).
        SetupSchedule(batteryLevel: 12, consumptionPerQuarter: 0);
        var helper = CreateHelper(new MemoryCache(new MemoryCacheOptions()));

        await helper.UpdateChargingSchedule(CancellationToken.None);

        A.CallTo(() => _pricesRepository.SetEnergyPriceShouldCharge(A<Guid>._, A<bool>._)).MustNotHaveHappened();
        A.CallTo(() => _pricesRepository.ClearShouldChargeFlags(A<DateTime>._)).MustNotHaveHappened();
    }

    private void SetupSchedule(int batteryLevel, int consumptionPerQuarter)
    {
        A.CallTo(() => _modbusService.GetBatteryLevel())
            .Returns(new BatteryLevel { Level = batteryLevel, MaxEnergy = 10000 });
        A.CallTo(() => _solarPowerHistoryRepository.GetAverageEnergyConsumption(A<DateTime>._, A<CancellationToken>._))
            .Returns(Enumerable.Range(0, 96)
                .Select(i => new ConsumptionPerFifteenMinutes { TimeOfDay = TimeSpan.FromMinutes(i * 15), Consumption = consumptionPerQuarter })
                .ToList());
        A.CallTo(() => _pricesRepository.GetCheapestEnergyPriceForTimestamp(A<DateTime>._))
            .Returns(new DayAheadCheapestEnergyPricesEntry { EuroPerMWh = 100 });
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
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["GRID_CHARGING_POWER"] = "2000" })
            .Build();

        return new HomeBatteryChargingHelper(
            A.Fake<IForecastService>(),
            _modbusService,
            configuration,
            cache,
            _pricesRepository,
            _solarPowerHistoryRepository,
            A.Fake<IEnergyForecastsRepository>(),
            _flagRepository,
            NullLogger<HomeBatteryChargingHelper>.Instance);
    }
}
