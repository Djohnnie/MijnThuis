using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Power;
using MijnThuis.DataAccess;
using MijnThuis.DataAccess.Repositories;

namespace MijnThuis.Application.Power.Queries;

public class GetDayAheadEnergyPricesQueryHandler : IRequestHandler<GetDayAheadEnergyPricesQuery, GetDayAheadEnergyPricesResponse>
{
    private readonly MijnThuisDbContext _dbContext;
    private readonly IFlagRepository _flagRepository;

    public GetDayAheadEnergyPricesQueryHandler(
        MijnThuisDbContext dbContext,
        IFlagRepository flagRepository)
    {
        _dbContext = dbContext;
        _flagRepository = flagRepository;
    }

    public async Task<GetDayAheadEnergyPricesResponse> Handle(GetDayAheadEnergyPricesQuery request, CancellationToken cancellationToken)
    {
        var from = request.Date.Date;
        var to = request.Date.Date.AddDays(1).AddSeconds(-1);

        var entries = await _dbContext.DayAheadEnergyPrices
            .Where(x => x.From >= from && x.To <= to)
            .OrderBy(x => x.From)
            .Select(x => new DayAheadEnergyPrice
            {
                Date = x.From,
                Price = x.EuroPerMWh / 1000M * 100M, // Convert to cents per kWh.
                ConsumptionPrice = Math.Round(x.ConsumptionCentsPerKWh * 1.06M, 3), // Add 6% VAT.
                InjectionPrice = x.InjectionCentsPerKWh // No VAT on injection.
            })
            .ToListAsync();

        var flag = await _flagRepository.GetElectricityTariffDetailsFlag();

        return new GetDayAheadEnergyPricesResponse
        {
            Entries = entries.Select(x => new DayAheadEnergyPrice
            {
                Date = x.Date,
                Price = x.Price,
                ConsumptionPrice = x.ConsumptionPrice,
                RealConsumptionPrice = x.ConsumptionPrice + flag.GreenEnergyContribution + flag.UsageTariff + flag.SpecialExciseTax + flag.EnergyContribution,
                InjectionPrice = x.InjectionPrice
            }).ToList()
        };
    }
}