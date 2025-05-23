using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Solar.Queries;

public class GetBatteryLevelTodayQuery : IRequest<GetBatteryLevelTodayResponse>
{

}

public class GetBatteryLevelTodayResponse
{
    public List<BatteryLevelEntry> Entries { get; set; } = new();
}

public class BatteryLevelEntry
{
    public DateTime Date { get; set; }
    public int? LevelOfCharge { get; set; }
}

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

        for (int i = 0; i < 24 * 12; i++)
        {
            var timeStamp = today.AddMinutes(5 * i);

            var entry = entries.FirstOrDefault(x => x.Date.AddMinutes(-5) < timeStamp && x.Date.AddMinutes(5) > timeStamp);

            if (entry is not null)
            {
                result.Entries.Add(new BatteryLevelEntry { Date = timeStamp, LevelOfCharge = (int)Math.Round(entry.StateOfCharge) });
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