using MijnThuis.DataAccess.Entities;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Worker.Helpers;

// Integrates instantaneous Modbus power readings into quarter-hour energy entries,
// mirroring the layout of the SolarEdge energy history so the charging schedule
// can be calculated without the SolarEdge API.
public class ModbusEnergyAccumulator
{
    public static readonly TimeSpan SlotDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MaxSampleGap = TimeSpan.FromMinutes(5);

    private readonly Dictionary<DateTime, SolarEnergyHistoryEntry> _slots = new();
    private (DateTime Timestamp, ModbusDataSet Data)? _previous;

    public IReadOnlyList<SolarEnergyHistoryEntry> AddSample(DateTime timestamp, ModbusDataSet data)
    {
        if (_previous is { } previous && timestamp > previous.Timestamp && timestamp - previous.Timestamp <= MaxSampleGap)
        {
            Integrate(previous.Timestamp, timestamp, previous.Data);
        }

        _previous = (timestamp, data);

        return FlushCompletedSlots(timestamp);
    }

    public static DateTime GetSlotStart(DateTime timestamp)
    {
        return new DateTime(timestamp.Ticks - timestamp.Ticks % SlotDuration.Ticks, timestamp.Kind);
    }

    private void Integrate(DateTime from, DateTime to, ModbusDataSet data)
    {
        var flows = PowerFlows.From(data);
        var segmentStart = from;

        while (segmentStart < to)
        {
            var slotStart = GetSlotStart(segmentStart);
            var segmentEnd = slotStart + SlotDuration < to ? slotStart + SlotDuration : to;
            var hours = (decimal)(segmentEnd - segmentStart).TotalHours;

            if (!_slots.TryGetValue(slotStart, out var slot))
            {
                slot = new SolarEnergyHistoryEntry { Id = Guid.NewGuid(), Date = slotStart };
                _slots[slotStart] = slot;
            }

            slot.Production += flows.Production * hours;
            slot.ProductionToHome += flows.ProductionToHome * hours;
            slot.ProductionToBattery += flows.ProductionToBattery * hours;
            slot.ProductionToGrid += flows.ProductionToGrid * hours;
            slot.Consumption += flows.Consumption * hours;
            slot.ConsumptionFromSolar += flows.ConsumptionFromSolar * hours;
            slot.ConsumptionFromBattery += flows.ConsumptionFromBattery * hours;
            slot.ConsumptionFromGrid += flows.ConsumptionFromGrid * hours;
            slot.Import += flows.Import * hours;
            slot.Export += flows.Export * hours;
            slot.ImportToBattery += flows.ImportToBattery * hours;

            segmentStart = segmentEnd;
        }
    }

    private IReadOnlyList<SolarEnergyHistoryEntry> FlushCompletedSlots(DateTime timestamp)
    {
        var completed = _slots
            .Where(x => x.Key + SlotDuration <= timestamp)
            .OrderBy(x => x.Key)
            .Select(x => x.Value)
            .ToList();

        foreach (var slot in completed)
        {
            _slots.Remove(slot.Date);
            slot.DataCollected = timestamp;
            slot.Production = Round(slot.Production);
            slot.ProductionToHome = Round(slot.ProductionToHome);
            slot.ProductionToBattery = Round(slot.ProductionToBattery);
            slot.ProductionToGrid = Round(slot.ProductionToGrid);
            slot.Consumption = Round(slot.Consumption);
            slot.ConsumptionFromSolar = Round(slot.ConsumptionFromSolar);
            slot.ConsumptionFromBattery = Round(slot.ConsumptionFromBattery);
            slot.ConsumptionFromGrid = Round(slot.ConsumptionFromGrid);
            slot.Import = Round(slot.Import);
            slot.Export = Round(slot.Export);
            slot.ImportToBattery = Round(slot.ImportToBattery);
        }

        return completed;
    }

    private static decimal Round(decimal value) => Math.Round(value, 3);

    // Modbus sign conventions: battery power > 0 is charging, grid power > 0 is export.
    private readonly record struct PowerFlows(
        decimal Production,
        decimal ProductionToHome,
        decimal ProductionToBattery,
        decimal ProductionToGrid,
        decimal Consumption,
        decimal ConsumptionFromSolar,
        decimal ConsumptionFromBattery,
        decimal ConsumptionFromGrid,
        decimal Import,
        decimal Export,
        decimal ImportToBattery)
    {
        public static PowerFlows From(ModbusDataSet data)
        {
            var production = Math.Max(0M, data.CurrentSolarPower);
            var consumption = Math.Max(0M, data.CurrentConsumptionPower);
            var batteryCharge = Math.Max(0M, data.CurrentBatteryPower);
            var batteryDischarge = Math.Max(0M, -data.CurrentBatteryPower);
            var export = Math.Max(0M, data.CurrentGridPower);
            var import = Math.Max(0M, -data.CurrentGridPower);

            var consumptionFromSolar = Math.Min(consumption, production);
            var consumptionFromBattery = Math.Min(consumption - consumptionFromSolar, batteryDischarge);
            var consumptionFromGrid = Math.Min(consumption - consumptionFromSolar - consumptionFromBattery, import);

            var productionToHome = consumptionFromSolar;
            var productionToBattery = Math.Min(production - productionToHome, batteryCharge);
            var productionToGrid = Math.Min(production - productionToHome - productionToBattery, export);

            return new PowerFlows(
                production,
                productionToHome,
                productionToBattery,
                productionToGrid,
                consumption,
                consumptionFromSolar,
                consumptionFromBattery,
                consumptionFromGrid,
                import,
                export,
                import - consumptionFromGrid);
        }
    }
}
