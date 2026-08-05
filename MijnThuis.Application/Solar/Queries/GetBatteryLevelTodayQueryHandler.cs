using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Solar.Queries;

public class GetBatteryLevelTodayQueryHandler : IRequestHandler<GetBatteryLevelTodayQuery, GetBatteryLevelTodayResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetBatteryLevelTodayQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetBatteryLevelTodayResponse> Handle(GetBatteryLevelTodayQuery request, CancellationToken cancellationToken)
    {
        var to = DateTime.Now;
        var from = to.AddHours(-24);

        var entries = await _dbContext.BatteryEnergyHistory
            .Where(x => x.Date >= from && x.Date <= to)
            .OrderBy(x => x.Date)
            .ToListAsync();

        var result = new GetBatteryLevelTodayResponse
        {
            Entries = new List<BatteryLevelEntry>()
        };

        var totalSlots = (int)Math.Ceiling((to - from).TotalMinutes / 15d);
        for (int i = 0; i <= totalSlots; i++)
        {
            var timeStamp = from.AddMinutes(15 * i);

            var entry = entries.Where(x => x.Date.AddMinutes(-15) < timeStamp && x.Date.AddMinutes(15) > timeStamp);

            if (entry.Count() > 0)
            {
                result.Entries.Add(new BatteryLevelEntry
                {
                    Date = timeStamp,
                    LevelOfCharge = (int)Math.Round(entry.Average(x => x.StateOfCharge)),
                    StateOfHealth = (int)Math.Round(entry.Average(x => x.CalculatedStateOfHealth * 100))
                });
            }
            else
            {
                result.Entries.Add(new BatteryLevelEntry { Date = timeStamp, LevelOfCharge = null, StateOfHealth = null });
            }
        }

        return result;
    }
}