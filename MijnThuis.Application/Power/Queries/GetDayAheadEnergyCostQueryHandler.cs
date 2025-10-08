using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.DataAccess;
using MijnThuis.DataAccess.Repositories;

namespace MijnThuis.Application.Power.Queries;

public class GetDayAheadEnergyCostQuery : IRequest<GetDayAheadEnergyCostResponse>
{
    public DateTime Date { get; set; }
}

public class GetDayAheadEnergyCostResponse
{
    public List<DayAheadEnergyCost> Entries { get; set; } = new();
}

public class DayAheadEnergyCost
{
    public DateTime Date { get; set; }
    public decimal? Consumption { get; set; }
    public decimal? ConsumptionCost { get; set; }
    public decimal ConsumptionPrice { get; set; }
    public int? BatteryLevel { get; set; }
}

internal class GetDayAheadEnergyCostQueryHandler : IRequestHandler<GetDayAheadEnergyCostQuery, GetDayAheadEnergyCostResponse>
{
    private readonly MijnThuisDbContext _dbContext;
    private readonly IFlagRepository _flagRepository;

    public GetDayAheadEnergyCostQueryHandler(
        MijnThuisDbContext dbContext,
        IFlagRepository flagRepository)
    {
        _dbContext = dbContext;
        _flagRepository = flagRepository;
    }

    public async Task<GetDayAheadEnergyCostResponse> Handle(GetDayAheadEnergyCostQuery request, CancellationToken cancellationToken)
    {
        var from = request.Date.Date;
        var to = request.Date.Date.AddDays(1).AddSeconds(-1);

        var priceEntries = await _dbContext.DayAheadEnergyPrices
            .Where(x => x.From >= from && x.To <= to)
            .OrderBy(x => x.From)
            .Select(x => new
            {
                x.From,
                ConsumptionPrice = Math.Round(x.ConsumptionCentsPerKWh * 1.06M, 3), // Add 6% VAT.
            })
            .ToListAsync();

        var consumptionEntries = await _dbContext.EnergyHistory
            .Where(x => x.Date >= from && x.Date <= to)
            .OrderBy(x => x.Date)
            .Select(x => new
            {
                From = x.Date.AddMinutes(-15),
                Consumption = x.TotalImportDelta,
                ImportCost = x.CalculatedImportCost
            })
            .ToListAsync();

        var batteryEntries = await _dbContext.SolarPowerHistory
            .Where(x => x.Date >= from && x.Date <= to)
            .OrderBy(x => x.Date)
            .Select(x => new
            {
                From = x.Date.AddMinutes(-15),
                BatteryLevel = x.StorageLevel * 100
            })
            .ToListAsync();

        var flag = await _flagRepository.GetElectricityTariffDetailsFlag();

        var result = new GetDayAheadEnergyCostResponse
        {
            Entries = new List<DayAheadEnergyCost>()
        };

        for (var i = 0; i < 96; i++)
        {
            var date = from.AddMinutes(i * 15);
            var priceEntry = priceEntries.FirstOrDefault(x => x.From == date);
            var consumptionEntry = consumptionEntries.FirstOrDefault(x => x.From == date);
            var batteryEntry = batteryEntries.FirstOrDefault(x => x.From == date);

            var entry = new DayAheadEnergyCost { Date = date };

            if (priceEntry != null)
            {
                entry.ConsumptionPrice = priceEntry.ConsumptionPrice + flag.GreenEnergyContribution + flag.UsageTariff + flag.SpecialExciseTax + flag.EnergyContribution;
            }

            if (consumptionEntry != null)
            {
                entry.Consumption = consumptionEntry.Consumption;
                entry.ConsumptionCost = consumptionEntry.ImportCost;
            }

            if (batteryEntry != null)
            {
                entry.BatteryLevel = (int)batteryEntry.BatteryLevel;
            }

            result.Entries.Add(entry);
        }

        return result;
    }
}