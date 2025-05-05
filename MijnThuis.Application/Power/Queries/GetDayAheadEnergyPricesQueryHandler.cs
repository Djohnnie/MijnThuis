using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Power;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Power.Queries;

public class GetDayAheadEnergyPricesQueryHandler : IRequestHandler<GetDayAheadEnergyPricesQuery, GetDayAheadEnergyPricesResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetDayAheadEnergyPricesQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetDayAheadEnergyPricesResponse> Handle(GetDayAheadEnergyPricesQuery request, CancellationToken cancellationToken)
    {
        var from = request.Date.Date;
        var to = request.Date.Date.AddHours(23);

        var entries = await _dbContext.DayAheadEnergyPrices
            .Where(x => x.From >= from && x.From <= to)
            .OrderBy(x => x.From)
            .Select(x => new DayAheadEnergyPrice
            {
                Date = x.From,
                Price = x.EuroPerMWh / 1000M * 100M // Convert to cents per kWh.
            })
            .ToListAsync();

        return new GetDayAheadEnergyPricesResponse { Entries = entries };
    }
}