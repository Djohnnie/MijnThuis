using MijnThuis.DataAccess.Entities;

namespace MijnThuis.DataAccess.Repositories;

public interface ICarChargingEnergyHistoryRepository
{
    Task Add(CarChargingEnergyHistoryEntry entry);
}

public class CarChargingEnergyHistoryRepository : ICarChargingEnergyHistoryRepository
{
    private readonly MijnThuisDbContext _dbContext;

    public CarChargingEnergyHistoryRepository(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(CarChargingEnergyHistoryEntry entry)
    {
        _dbContext.CarChargingHistory.Add(entry);
        await _dbContext.SaveChangesAsync();
    }
}