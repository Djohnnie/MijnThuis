using Microsoft.EntityFrameworkCore;
using MijnThuis.DataAccess.Entities;

namespace MijnThuis.DataAccess.Repositories;

public interface IFlagRepository
{
    Task<Flag?> GetFlag(string name);
    Task SetFlag<TFlag>(string name, TFlag flag) where TFlag : IFlag;

    Task<ManualCarChargeFlag> GetManualCarChargeFlag();
    Task SetCarChargingFlag(bool shouldCharge, int chargeAmps);

    Task<ConsumptionTariffExpressionFlag> GetConsumptionTariffExpressionFlag();
    Task SetConsumptionTariffExpressionFlag(string expression, string source);

    Task<InjectionTariffExpressionFlag> GetInjectionTariffExpressionFlag();
    Task SetInjectionTariffExpressionFlag(string expression, string source);

    Task<SamsungTheFrameTokenFlag> GetSamsungTheFrameTokenFlag();
    Task SetSamsungTheFrameTokenFlag(string token, TimeSpan autoOn, TimeSpan autoOff);
}

public class FlagRepository : IFlagRepository
{
    private readonly MijnThuisDbContext _dbContext;

    public FlagRepository(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Flag?> GetFlag(string name)
    {
        return await _dbContext.Flags.AsNoTracking().SingleOrDefaultAsync(f => f.Name == name);
    }

    public async Task SetFlag<TFlag>(string name, TFlag flag) where TFlag : IFlag
    {
        var existingFlag = await GetFlag(name);
        if (existingFlag is null)
        {
            _dbContext.Flags.Add(new Flag
            {
                Id = Guid.NewGuid(),
                Name = name,
                Value = flag.Serialize()
            });
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            await _dbContext.Flags.Where(x => x.Name == name).ExecuteUpdateAsync(p => p.SetProperty(p => p.Value, flag.Serialize()));
        }
    }

    public async Task<ManualCarChargeFlag> GetManualCarChargeFlag()
    {
        var flag = await GetFlag(ManualCarChargeFlag.Name);
        return flag != null ? ManualCarChargeFlag.Deserialize(flag.Value) : ManualCarChargeFlag.Default;
    }

    public async Task SetCarChargingFlag(bool shouldCharge, int chargeAmps)
    {
        await SetFlag(ManualCarChargeFlag.Name, new ManualCarChargeFlag
        {
            ShouldCharge = shouldCharge,
            ChargeAmps = chargeAmps
        });
    }

    public async Task<ConsumptionTariffExpressionFlag> GetConsumptionTariffExpressionFlag()
    {
        var flag = await GetFlag(ConsumptionTariffExpressionFlag.Name);
        return flag != null ? ConsumptionTariffExpressionFlag.Deserialize(flag.Value) : ConsumptionTariffExpressionFlag.Default;
    }

    public async Task SetConsumptionTariffExpressionFlag(string expression, string source)
    {
        await SetFlag(ConsumptionTariffExpressionFlag.Name, new ConsumptionTariffExpressionFlag
        {
            Expression = expression,
            Source = source
        });
    }

    public async Task<InjectionTariffExpressionFlag> GetInjectionTariffExpressionFlag()
    {
        var flag = await GetFlag(InjectionTariffExpressionFlag.Name);
        return flag != null ? InjectionTariffExpressionFlag.Deserialize(flag.Value) : InjectionTariffExpressionFlag.Default;
    }

    public async Task SetInjectionTariffExpressionFlag(string expression, string source)
    {
        await SetFlag(InjectionTariffExpressionFlag.Name, new InjectionTariffExpressionFlag
        {
            Expression = expression,
            Source = source
        });
    }

    public async Task<SamsungTheFrameTokenFlag> GetSamsungTheFrameTokenFlag()
    {
        var flag = await GetFlag(SamsungTheFrameTokenFlag.Name);
        return flag != null ? SamsungTheFrameTokenFlag.Deserialize(flag.Value) : SamsungTheFrameTokenFlag.Default;
    }

    public async Task SetSamsungTheFrameTokenFlag(string token, TimeSpan autoOn, TimeSpan autoOff)
    {
        await SetFlag(SamsungTheFrameTokenFlag.Name, new SamsungTheFrameTokenFlag
        {
            Token = token,
            AutoOn = autoOn,
            AutoOff = autoOff
        });
    }
}