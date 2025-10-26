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
    public int? EstimatedBatteryLevel { get; set; }
    public bool ShouldCharge { get; set; }
    public decimal? ConsumptionPriceShouldCharge { get; set; }
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
                From = x.Date,
                BatteryLevel = x.StorageLevel * 100
            })
            .ToListAsync();

        var energyForecasts = await _dbContext.EnergyForecasts
            .Where(x => x.Date >= from && x.Date <= to)
            .OrderBy(x => x.Date)
            .ToListAsync();

        var dayAheadCheapestEntries = await _dbContext.DayAheadCheapestEnergyPrices
            .Where(x => x.From >= from && x.To <= to)
            .OrderBy(x => x.From)
            .ToListAsync();

        var flag = await _flagRepository.GetElectricityTariffDetailsFlag();

        var result = new GetDayAheadEnergyCostResponse
        {
            Entries = new List<DayAheadEnergyCost>()
        };

        var previousPrice = 0M;

        var previousEntryShouldCharge = false;
        for (var i = 0; i < 96; i++)
        {
            var date = from.AddMinutes(i * 15);
            var priceEntry = priceEntries.FirstOrDefault(x => x.From == date);
            var consumptionEntry = consumptionEntries.FirstOrDefault(x => x.From == date);
            var batteryEntry = batteryEntries.FirstOrDefault(x => x.From == date);
            var energyForecastEntry = energyForecasts.FirstOrDefault(x => x.Date == date);
            var dayAheadCheapestEntry = dayAheadCheapestEntries.FirstOrDefault(x => x.From == date);

            var entry = new DayAheadEnergyCost { Date = date };

            if (priceEntry != null)
            {
                entry.ConsumptionPrice = priceEntry.ConsumptionPrice + flag.GreenEnergyContribution + flag.UsageTariff + flag.SpecialExciseTax + flag.EnergyContribution;
                previousPrice = entry.ConsumptionPrice;
            }
            else
            {
                entry.ConsumptionPrice = previousPrice;
            }

            if (consumptionEntry != null)
            {
                entry.Consumption = consumptionEntry.Consumption > 0M ? consumptionEntry.Consumption : null;
                entry.ConsumptionCost = consumptionEntry.ImportCost > 0M ? consumptionEntry.ImportCost : null;
            }

            if (batteryEntry != null)
            {
                entry.BatteryLevel = (int)batteryEntry.BatteryLevel;
            }
            else
            {
                if (entry.Date >= DateTime.Now.AddHours(-1) && energyForecastEntry != null)
                {
                    entry.EstimatedBatteryLevel = energyForecastEntry.EstimatedBatteryLevel;
                }
            }

            if (dayAheadCheapestEntry != null)
            {
                entry.ShouldCharge = dayAheadCheapestEntry.ShouldCharge;
                if (entry.ShouldCharge /*|| previousEntryShouldCharge*/)
                {
                    entry.ConsumptionPriceShouldCharge = 0.0M;
                }

                previousEntryShouldCharge = entry.ShouldCharge;
            }

            result.Entries.Add(entry);
        }

        return result;
    }
}