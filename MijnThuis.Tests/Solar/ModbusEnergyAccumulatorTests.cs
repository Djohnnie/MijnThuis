using MijnThuis.Integrations.Solar;
using MijnThuis.Worker.Helpers;
using Shouldly;

namespace MijnThuis.Tests.Solar;

public class ModbusEnergyAccumulatorTests
{
    private static readonly DateTime SlotStart = new(2026, 10, 27, 12, 0, 0);

    [Fact]
    public void CompletesSlotWithIntegratedEnergyWhenNextSlotStarts()
    {
        var accumulator = new ModbusEnergyAccumulator();
        var sample = Sample(solar: 0, consumption: 1200, battery: -800, grid: -400);

        for (var minute = 0; minute < 15; minute++)
        {
            accumulator.AddSample(SlotStart.AddMinutes(minute), sample).ShouldBeEmpty();
        }

        var completed = accumulator.AddSample(SlotStart.AddMinutes(15), sample);

        var entry = completed.ShouldHaveSingleItem();
        entry.Date.ShouldBe(SlotStart);
        entry.Consumption.ShouldBe(300M);
        entry.ConsumptionFromBattery.ShouldBe(200M);
        entry.ConsumptionFromGrid.ShouldBe(100M);
        entry.ConsumptionFromSolar.ShouldBe(0M);
        entry.Import.ShouldBe(100M);
        entry.Export.ShouldBe(0M);
        entry.Production.ShouldBe(0M);
        entry.ImportToBattery.ShouldBe(0M);
    }

    [Fact]
    public void SplitsProductionBetweenHomeBatteryAndGrid()
    {
        var accumulator = new ModbusEnergyAccumulator();
        var sample = Sample(solar: 4000, consumption: 1000, battery: 2000, grid: 1000);

        FeedEveryMinute(accumulator, sample, SlotStart, SlotStart.AddMinutes(14));
        var completed = accumulator.AddSample(SlotStart.AddMinutes(15), sample);

        var entry = completed.ShouldHaveSingleItem();
        entry.Production.ShouldBe(1000M);
        entry.ProductionToHome.ShouldBe(250M);
        entry.ProductionToBattery.ShouldBe(500M);
        entry.ProductionToGrid.ShouldBe(250M);
        entry.Export.ShouldBe(250M);
        entry.Import.ShouldBe(0M);
    }

    [Fact]
    public void GridChargingIsAttributedToImportToBattery()
    {
        var accumulator = new ModbusEnergyAccumulator();
        var sample = Sample(solar: 0, consumption: 400, battery: 2000, grid: -2400);

        FeedEveryMinute(accumulator, sample, SlotStart, SlotStart.AddMinutes(14));
        var entry = accumulator.AddSample(SlotStart.AddMinutes(15), sample).ShouldHaveSingleItem();

        entry.Import.ShouldBe(600M);
        entry.ConsumptionFromGrid.ShouldBe(100M);
        entry.ImportToBattery.ShouldBe(500M);
    }

    [Fact]
    public void SampleSpanningSlotBoundaryIsSplitBetweenSlots()
    {
        var accumulator = new ModbusEnergyAccumulator();
        var sample = Sample(solar: 0, consumption: 1200, battery: 0, grid: -1200);

        accumulator.AddSample(SlotStart.AddMinutes(13), sample);
        var completed = accumulator.AddSample(SlotStart.AddMinutes(16), sample);

        completed.ShouldHaveSingleItem().Consumption.ShouldBe(40M);

        FeedEveryMinute(accumulator, sample, SlotStart.AddMinutes(17), SlotStart.AddMinutes(29));
        accumulator.AddSample(SlotStart.AddMinutes(30), sample).ShouldHaveSingleItem().Consumption.ShouldBe(300M);
    }

    [Fact]
    public void LargeGapsBetweenSamplesAreNotIntegrated()
    {
        var accumulator = new ModbusEnergyAccumulator();
        var sample = Sample(solar: 0, consumption: 1200, battery: 0, grid: -1200);

        accumulator.AddSample(SlotStart, sample);
        var completed = accumulator.AddSample(SlotStart.AddMinutes(10), sample);

        completed.ShouldBeEmpty();
        accumulator.AddSample(SlotStart.AddMinutes(15), sample).ShouldHaveSingleItem().Consumption.ShouldBe(100M);
    }

    private static void FeedEveryMinute(ModbusEnergyAccumulator accumulator, ModbusDataSet sample, DateTime from, DateTime toInclusive)
    {
        for (var timestamp = from; timestamp <= toInclusive; timestamp = timestamp.AddMinutes(1))
        {
            accumulator.AddSample(timestamp, sample);
        }
    }

    private static ModbusDataSet Sample(decimal solar, decimal consumption, decimal battery, decimal grid)
    {
        return new ModbusDataSet
        {
            CurrentSolarPower = solar,
            CurrentConsumptionPower = consumption,
            CurrentBatteryPower = battery,
            CurrentGridPower = grid
        };
    }
}
