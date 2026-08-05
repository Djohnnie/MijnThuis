using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Solar.Queries;

public class GetBatteryLevelHistoryQuery : IRequest<GetBatteryLevelHistoryResponse>
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
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
            .Where(x => x.Date >= request.From && x.Date <= request.To)
            .OrderBy(x => x.Date)
            .ToListAsync();

        var result = new GetBatteryLevelHistoryResponse
        {
            Entries = new List<BatteryLevelEntry>()
        };

        var totalSlots = (int)Math.Ceiling((request.To - request.From).TotalMinutes / 15d);
        for (int i = 0; i <= totalSlots; i++)
        {
            var timeStamp = request.From.AddMinutes(15 * i);

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
        return result;
    }
}