using System.Buffers.Text;
using System.Text.Json;

namespace MijnThuis.DataAccess.Entities;

public interface IFlag
{
    string Serialize();
}

public class Flag
{
    public Guid Id { get; set; }
    public long SysId { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
}

public class ManualCarChargeFlag : IFlag
{
    public static string Name => "ManualCarCharge";

    public static ManualCarChargeFlag Default => new ManualCarChargeFlag();

    public bool ShouldCharge { get; set; }
    public int ChargeAmps { get; set; }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static ManualCarChargeFlag Deserialize(string json)
    {
        return JsonSerializer.Deserialize<ManualCarChargeFlag>(json) ?? Default;
    }
}

public class ManualHomeBatteryChargeFlag : IFlag
{
    public static string Name => "ManualHomeBatteryCharge";

    public static ManualHomeBatteryChargeFlag Default => new ManualHomeBatteryChargeFlag();

    public bool ShouldCharge { get; set; }
    public int ChargeWattage { get; set; }
    public DateTime ChargeUntil { get; set; }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static ManualHomeBatteryChargeFlag Deserialize(string json)
    {
        return JsonSerializer.Deserialize<ManualHomeBatteryChargeFlag>(json) ?? Default;
    }
}

public class ConsumptionTariffExpressionFlag : IFlag
{
    public static string Name => "ConsumptionTariffExpression";
    public static ConsumptionTariffExpressionFlag Default => new ConsumptionTariffExpressionFlag();
    public string Expression { get; set; }
    public string Source { get; set; }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static ConsumptionTariffExpressionFlag Deserialize(string json)
    {
        return JsonSerializer.Deserialize<ConsumptionTariffExpressionFlag>(json);
    }
}

public class InjectionTariffExpressionFlag : IFlag
{
    public static string Name => "InjectionTariffExpression";
    public static InjectionTariffExpressionFlag Default => new InjectionTariffExpressionFlag();
    public string Expression { get; set; }
    public string Source { get; set; }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static InjectionTariffExpressionFlag Deserialize(string json)
    {
        return JsonSerializer.Deserialize<InjectionTariffExpressionFlag>(json);
    }
}

public class ElectricityTariffDetailsFlag : IFlag
{
    public static string Name => "ElectricityTariffDetailsFlag";
    public static ElectricityTariffDetailsFlag Default => new ElectricityTariffDetailsFlag();
    public decimal FixedCharge { get; set; }
    public decimal GreenEnergyContribution { get; set; }
    public decimal CapacityTariff { get; set; }
    public decimal UsageTariff { get; set; }
    public decimal DataAdministration { get; set; }
    public decimal SpecialExciseTax { get; set; }
    public decimal EnergyContribution { get; set; }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static ElectricityTariffDetailsFlag Deserialize(string json)
    {
        return JsonSerializer.Deserialize<ElectricityTariffDetailsFlag>(json);
    }
}

public class SamsungTheFrameTokenFlag : IFlag
{
    public static string Name => "SamsungTheFrameToken";
    public static SamsungTheFrameTokenFlag Default => new SamsungTheFrameTokenFlag();
    public TimeSpan AutoOn { get; set; }
    public TimeSpan AutoOff { get; set; }
    public bool IsDisabled { get; set; }
    public string Token { get; set; }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static SamsungTheFrameTokenFlag Deserialize(string json)
    {
        return JsonSerializer.Deserialize<SamsungTheFrameTokenFlag>(json);
    }
}