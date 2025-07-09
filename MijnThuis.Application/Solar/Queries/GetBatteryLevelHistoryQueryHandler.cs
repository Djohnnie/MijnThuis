using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Solar.Queries;

public class GetBatteryLevelHistoryQuery : IRequest<GetBatteryLevelHistoryResponse>
{
    public DateTime Date { get; set; }
}

public class GetBatteryLevelHistoryResponse
{
    public List<BatteryLevelEntry> Entries { get; set; } = new();
}

public class GetBatteryLevelHistoryQueryHandler : IRequestHandler<GetBatteryLevelHistoryQuery, GetBatteryLevelHistoryResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetBatteryLevelHistoryQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetBatteryLevelHistoryResponse> Handle(GetBatteryLevelHistoryQuery request, CancellationToken cancellationToken)
    {
        var entries = await _dbContext.BatteryEnergyHistory
            .Where(x => x.Date.Date == request.Date.Date)
            .OrderBy(x => x.Date)
            .ToListAsync();

        var result = new GetBatteryLevelHistoryResponse
        {
            Entries = new List<BatteryLevelEntry>()
        };

        for (int i = 0; i < 24 * 4; i++)
        {
            var timeStamp = request.Date.Date.AddMinutes(15 * i);

            var entry = entries.Where(x => x.Date.AddMinutes(-15) < timeStamp && x.Date.AddMinutes(15) > timeStamp);

            if (entry.Count() > 0)
            {
                result.Entries.Add(new BatteryLevelEntry
                {
                    Date = timeStamp,
                    LevelOfCharge = (int)Math.Round(entry.Average(x => x.StateOfCharge))
                });
            }
            else
            {
                result.Entries.Add(new BatteryLevelEntry { Date = timeStamp, LevelOfCharge = null });
            }
        }

        result.Entries.Add(new BatteryLevelEntry { Date = request.Date.Date.AddDays(1), LevelOfCharge = null });

        return result;
    }
}