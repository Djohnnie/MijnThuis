using Microsoft.EntityFrameworkCore;
using MijnThuis.DataAccess.Entities;

namespace MijnThuis.DataAccess.Repositories;

public interface IFlagRepository
{
    Task<Flag?> GetFlag(string name);

    Task<ManualCarChargeFlag> GetManualCarChargeFlag();
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
        return await _dbContext.Flags.SingleOrDefaultAsync(f => f.Name == name);
    }

    public async Task<ManualCarChargeFlag> GetManualCarChargeFlag()
    {
        var flag = await GetFlag(ManualCarChargeFlag.Name);
        return flag != null ? ManualCarChargeFlag.Deserialize(flag.Value) : ManualCarChargeFlag.Default;
    }
}