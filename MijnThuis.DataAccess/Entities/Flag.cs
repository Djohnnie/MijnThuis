using System.Text.Json;

namespace MijnThuis.DataAccess.Entities;

public class Flag
{
    public Guid Id { get; set; }
    public long SysId { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
}

public class ManualCarChargeFlag
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
        return JsonSerializer.Deserialize<ManualCarChargeFlag>(json);
    }
}