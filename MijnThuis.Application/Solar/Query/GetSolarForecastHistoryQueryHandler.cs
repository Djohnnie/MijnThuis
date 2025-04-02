using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Solar.Query;

public class GetSolarForecastHistoryQueryHandler : IRequestHandler<GetSolarForecastHistoryQuery, GetSolarForecastHistoryResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetSolarForecastHistoryQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetSolarForecastHistoryResponse> Handle(GetSolarForecastHistoryQuery request, CancellationToken cancellationToken)
    {
        var from = new DateTime(request.Date.Year, request.Date.Month, 1);
        var to = from.AddMonths(1).AddDays(-1);

        var entries = await _dbContext.SolarForecastHistory
            .Where(x => x.Date.Date >= from && x.Date.Date <= to)
            .ToListAsync();

        var response = new GetSolarForecastHistoryResponse();

        for (var day = 1; day <= to.Day; day++)
        {
            var forecastEntries = entries
                .Where(x => x.Date.Day == day)
                .ToList();

            response.Entries.Add(new SolarForecastHistoryEntry
            {
                Date = new DateTime(request.Date.Year, request.Date.Month, day),
                ForecastedEnergyToday = forecastEntries.Any() ? forecastEntries.Sum(x => x.ForecastedEnergyToday) / 1000M : 0.0M,
                ActualEnergyToday = forecastEntries.Any() ? forecastEntries.Average(x => x.ActualEnergyToday) / 1000M : 0.0M,
                ForecastedEnergyTomorrow = forecastEntries.Any() ? forecastEntries.Sum(x => x.ForecastedEnergyTomorrow) / 1000M : 0.0M,
                ForecastedEnergyDayAfterTomorrow = forecastEntries.Any() ? forecastEntries.Sum(x => x.ForecastedEnergyDayAfterTomorrow) / 1000M : 0.0M
            });
        }

        return response;
    }
}