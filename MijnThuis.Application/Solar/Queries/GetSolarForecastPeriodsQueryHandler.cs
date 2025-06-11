using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Solar.Queries;

public class GetSolarForecastPeriodsQueryHandler : IRequestHandler<GetSolarForecastPeriodsQuery, GetSolarForecastPeriodsResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetSolarForecastPeriodsQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetSolarForecastPeriodsResponse> Handle(GetSolarForecastPeriodsQuery request, CancellationToken cancellationToken)
    {
        var date = request.Date.Date;
        var periods = await _dbContext.SolarForecastPeriods
            .Where(x => x.Timestamp.Date == date)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);

        var responsePeriods = new List<SolarForecastPeriod>();

        for (var i = 0; i < 48; i++)
        {
            var timestamp = date.AddMinutes(i * 30); // 30-minute intervals
            var period = periods.FirstOrDefault(x => x.Timestamp == timestamp);
            responsePeriods.Add(new SolarForecastPeriod
            {
                Timestamp = timestamp,
                ForecastedEnergy = period?.ForecastedEnergy ?? 0,
                ActualEnergy = period?.ActualEnergy ?? 0
            });
        }

        return new GetSolarForecastPeriodsResponse
        {
            Periods = responsePeriods
        };
    }
}