using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Power;
using MijnThuis.DataAccess;
using MijnThuis.DataAccess.Repositories;
using MijnThuis.Integrations.Power;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Application.Power.Queries;

public class GetPowerOverviewQueryHandler : IRequestHandler<GetPowerOverviewQuery, GetPowerOverviewResponse>
{
    private readonly MijnThuisDbContext _dbContext;
    private readonly IPowerService _powerService;
    private readonly IModbusService _modbusService;
    private readonly IDayAheadEnergyPricesRepository _energyPricesRepository;
    private readonly IFlagRepository _flagRepository;

    public GetPowerOverviewQueryHandler(
        MijnThuisDbContext dbContext,
        IPowerService powerService,
        IModbusService modbusService,
        IDayAheadEnergyPricesRepository energyPricesRepository,
        IFlagRepository flagRepository)
    {
        _dbContext = dbContext;
        _powerService = powerService;
        _modbusService = modbusService;
        _energyPricesRepository = energyPricesRepository;
        _flagRepository = flagRepository;
    }

    public async Task<GetPowerOverviewResponse> Handle(GetPowerOverviewQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var powerResult = await _powerService.GetOverview();
        var consumptionResult = await _modbusService.GetOverview();
        var energyToday = await _dbContext.EnergyHistory
            .Where(x => x.Date.Date == today)
            .Select(x => new
            {
                ImportToday = x.TotalImportDelta,
                ExportToday = x.TotalExportDelta,
                ImportCost = x.CalculatedImportCost,
                ExportCost = x.CalculatedExportCost,
                TotalCost = x.CalculatedTotalCost,
                TotalVariableCost = x.CalculatedImportCost + x.CalculatedVariableCost
            }).ToListAsync();
        var energyThisMonth = await _dbContext.EnergyHistory
            .Where(x => x.Date.Date >= new DateTime(today.Year, today.Month, 1) && x.Date.Date <= tomorrow)
            .Select(x => new
            {
                ImportToday = x.TotalImportDelta,
                ExportToday = x.TotalExportDelta,
                ImportCost = x.CalculatedImportCost,
                ExportCost = x.CalculatedExportCost,
                TotalCost = x.CalculatedTotalCost,
                TotalVariableCost = x.CalculatedImportCost + x.CalculatedVariableCost
            }).ToListAsync();
        var energyThisYear = await _dbContext.EnergyHistory
            .Where(x => x.Date.Date >= new DateTime(today.Year, 1, 1) && x.Date.Date <= tomorrow)
            .Select(x => new
            {
                ImportToday = x.TotalImportDelta,
                ExportToday = x.TotalExportDelta,
                ImportCost = x.CalculatedImportCost,
                ExportCost = x.CalculatedExportCost,
                TotalCost = x.CalculatedTotalCost,
                TotalVariableCost = x.CalculatedImportCost + x.CalculatedVariableCost
            }).ToListAsync();
        var negativePriceRange = await _energyPricesRepository.GetNegativeInjectionPriceRange();
        var energyPricing = await _energyPricesRepository.GetEnergyPriceForTimestamp(DateTime.Now);
        var electricityFlag = await _flagRepository.GetElectricityTariffDetailsFlag();

        var result = powerResult.Adapt<GetPowerOverviewResponse>();
        result.Description = negativePriceRange.Description;
        result.CurrentConsumption = consumptionResult.CurrentConsumptionPower / 1000M;
        result.ImportToday = energyToday.Sum(x => x.ImportToday);
        result.ExportToday = energyToday.Sum(x => x.ExportToday);
        result.ImportThisMonth = energyThisMonth.Sum(x => x.ImportToday);
        result.ExportThisMonth = energyThisMonth.Sum(x => x.ExportToday);
        result.CurrentPricePeriod = $"({energyPricing.From:HHumm} - {energyPricing.To.AddSeconds(1):HHumm})";
        result.CurrentConsumptionPrice = energyPricing.ConsumptionCentsPerKWh;
        result.CurrentConsumptionPrice = Math.Round(energyPricing.ConsumptionCentsPerKWh * 1.06M, 3) + electricityFlag.GreenEnergyContribution + electricityFlag.UsageTariff + electricityFlag.SpecialExciseTax + electricityFlag.EnergyContribution;
        result.CurrentInjectionPrice = energyPricing.InjectionCentsPerKWh;
        result.CostToday = energyToday.Sum(x => x.TotalCost);
        result.CostThisMonth = energyThisMonth.Sum(x => x.TotalCost);
        result.AverageCostPerKwhToday = energyToday.Sum(x => x.TotalVariableCost) / energyToday.Sum(x => x.ImportToday) * 100M;
        result.AverageCostPerKwhThisMonth = energyThisMonth.Sum(x => x.TotalVariableCost) / energyThisMonth.Sum(x => x.ImportToday) * 100M;
        result.AverageCostPerKwhThisYear = energyThisYear.Sum(x => x.TotalVariableCost) / energyThisYear.Sum(x => x.ImportToday) * 100M;

        return result;
    }
}