using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Power.Queries;

public class GetPeakPowerUsageHistoryQuery : IRequest<GetPeakPowerUsageHistoryResponse>
{
    public int Year { get; set; }
}

public class GetPeakPowerUsageHistoryResponse
{
    public List<MonthlyPowerPeak> Entries { get; set; } = new();
}

public class MonthlyPowerPeak
{
    public DateTime Date { get; set; }
    public decimal PowerPeak { get; set; }
}

public class GetPeakPowerUsageHistoryQueryHandler : IRequestHandler<GetPeakPowerUsageHistoryQuery, GetPeakPowerUsageHistoryResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetPeakPowerUsageHistoryQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetPeakPowerUsageHistoryResponse> Handle(GetPeakPowerUsageHistoryQuery request, CancellationToken cancellationToken)
    {
        var from = new DateTime(request.Year, 1, 1);
        var to = new DateTime(request.Year, 12, 31);

        // Skip the first entry in each month because it still has data from the previous month
        var entries = await _dbContext.EnergyHistory
            .Where(x => x.Date.Date >= from && x.Date.Date <= to)
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .Select(x => new MonthlyPowerPeak
            {
                Date = new DateTime(x.Key.Year, x.Key.Month, 1),
                PowerPeak = x.Skip(1).Select(y => y.MonthlyPowerPeak).Max(),
            })
            .ToListAsync();

        // If there are months without data, fill them with 0
        var allMonths = Enumerable.Range(1, 12).Select(m => new DateTime(request.Year, m, 1)).ToList();
        foreach (var month in allMonths.Except(entries.Select(e => e.Date)).ToList())
        {
            entries.Add(new MonthlyPowerPeak
            {
                Date = month,
                PowerPeak = month < DateTime.Now ? 2.5M : 0M
            });
        }

        return new GetPeakPowerUsageHistoryResponse { Entries = entries.OrderBy(x => x.Date).ToList() };
    }
}