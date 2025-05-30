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
        var today = DateTime.Today;

        var entries = await _dbContext.BatteryEnergyHistory
            .Where(x => x.Date.Date == today)
            .OrderBy(x => x.Date)
            .ToListAsync();

        var result = new GetBatteryLevelTodayResponse
        {
            Entries = new List<BatteryLevelEntry>()
        };

        for (int i = 0; i < 24 * 4; i++)
        {
            var timeStamp = today.AddMinutes(15 * i);

            var entry = entries.Where(x => x.Date.AddMinutes(-15) < timeStamp && x.Date.AddMinutes(15) > timeStamp);

            if (entry.Count() > 0)
            {
                result.Entries.Add(new BatteryLevelEntry { Date = timeStamp, LevelOfCharge = (int)Math.Round(entry.Average(x => x.StateOfCharge)) });
            }
            else
            {
                result.Entries.Add(new BatteryLevelEntry { Date = timeStamp, LevelOfCharge = null });
            }
        }

        result.Entries.Add(new BatteryLevelEntry { Date = today.AddDays(1), LevelOfCharge = null });

        return result;
    }
}