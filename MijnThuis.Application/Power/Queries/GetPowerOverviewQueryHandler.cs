using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Power;
using MijnThuis.DataAccess;
using MijnThuis.DataAccess.Entities;
using MijnThuis.DataAccess.Repositories;
using MijnThuis.Integrations.Power;
using MijnThuis.Integrations.Samsung;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Application.Power.Queries;

public class GetPowerOverviewQueryHandler : IRequestHandler<GetPowerOverviewQuery, GetPowerOverviewResponse>
{
    private readonly MijnThuisDbContext _dbContext;
    private readonly IPowerService _powerService;
    private readonly IShellyService _shellyService;
    private readonly IModbusService _modbusService;
    private readonly ISamsungService _samsungService;
    private readonly IDayAheadEnergyPricesRepository _energyPricesRepository;
    private readonly IFlagRepository _flagRepository;

    public GetPowerOverviewQueryHandler(
        MijnThuisDbContext dbContext,
        IPowerService powerService,
        IShellyService shellyService,
        IModbusService modbusService,
        ISamsungService samsungService,
        IDayAheadEnergyPricesRepository energyPricesRepository,
        IFlagRepository flagRepository)
    {
        _dbContext = dbContext;
        _powerService = powerService;
        _shellyService = shellyService;
        _modbusService = modbusService;
        _samsungService = samsungService;
        _energyPricesRepository = energyPricesRepository;
        _flagRepository = flagRepository;
    }

    public async Task<GetPowerOverviewResponse> Handle(GetPowerOverviewQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var powerResult = await _powerService.GetOverview();
        var consumptionResult = await _modbusService.GetOverview();
        var energyToday = await _dbContext.EnergyHistory
            .Where(x => x.Date.Date == today)
            .Select(x => new
            {
                ImportToday = x.TotalImportDelta,
                ExportToday = x.TotalExportDelta
            }).ToListAsync();
        var energyCostToday = await _dbContext.EnergyHistory
            .Where(x => x.Date.Date == today)
            .Select(x => new
            {
                ImportCost = x.CalculatedImportCost,
                ExportCost = x.CalculatedExportCost
            }).ToListAsync();
        var energyThisMonth = await _dbContext.EnergyHistory
            .Where(x => x.Date.Date >= new DateTime(today.Year, today.Month, 1) && x.Date.Date <= today)
            .Select(x => new
            {
                ImportToday = x.TotalImportDelta,
                ExportToday = x.TotalExportDelta
            }).ToListAsync();

        var energyCostThisMonth = await _dbContext.EnergyHistory
            .Where(x => x.Date.Date >= new DateTime(today.Year, today.Month, 1) && x.Date.Date <= today)
            .Select(x => new
            {
                ImportCost = x.CalculatedImportCost,
                ExportCost = x.CalculatedExportCost
            }).ToListAsync();
        var negativePriceRange = await _energyPricesRepository.GetNegativeInjectionPriceRange();
        var energyPricing = await _energyPricesRepository.GetEnergyPriceForTimestamp(DateTime.Now);
        var tvPowerSwitchOverview = await _shellyService.GetTvPowerSwitchOverview();
        var bureauPowerSwitchOverview = await _shellyService.GetBureauPowerSwitchOverview();
        var vijverPowerSwitchOverview = await _shellyService.GetVijverPowerSwitchOverview();
        var electricityFlag = await _flagRepository.GetElectricityTariffDetailsFlag();

        var result = powerResult.Adapt<GetPowerOverviewResponse>();
        result.Description = negativePriceRange.Description;
        result.CurrentConsumption = consumptionResult.CurrentConsumptionPower / 1000M;
        result.ImportToday = energyToday.Sum(x => x.ImportToday);
        result.ExportToday = energyToday.Sum(x => x.ExportToday);
        result.ImportThisMonth = energyThisMonth.Sum(x => x.ImportToday);
        result.ExportThisMonth = energyThisMonth.Sum(x => x.ExportToday);
        result.CurrentPricePeriod = $"({energyPricing.From:HHu} - {energyPricing.To.AddSeconds(1):HHu})";
        result.CurrentConsumptionPrice = energyPricing.ConsumptionCentsPerKWh;
        result.CurrentInjectionPrice = energyPricing.InjectionCentsPerKWh;
        result.IsTvOn = tvPowerSwitchOverview.IsOn;
        result.IsBureauOn = bureauPowerSwitchOverview.IsOn;
        result.IsVijverOn = vijverPowerSwitchOverview.IsOn;
        result.IsTheFrameOn = await _samsungService.IsTheFrameOn();
        result.CostToday = await CalculateCost(electricityFlag, result.ImportToday, result.ExportToday, energyCostToday.Sum(x => x.ImportCost) + energyCostToday.Sum(x => x.ExportCost), daily: true);
        result.CostThisMonth = await CalculateCost(electricityFlag, result.ImportThisMonth, result.ExportThisMonth, energyCostThisMonth.Sum(x => x.ImportCost) + energyCostThisMonth.Sum(x => x.ExportCost), daily: false);

        return result;
    }

    private async Task<decimal> CalculateCost(ElectricityTariffDetailsFlag electricityFlag, decimal importToday, decimal exportToday, decimal energyCost, bool daily)
    {
        // Skip the first entry in each month because it still has data from the previous month
        var to = DateTime.Today;
        var from = to.AddMonths(-6);
        from = new DateTime(from.Year, from.Month, 1);
        var entries = await _dbContext.EnergyHistory
            .Where(x => x.Date.Date >= from && x.Date.Date <= to)
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .Select(x => new MonthlyPowerPeak
            {
                Date = new DateTime(x.Key.Year, x.Key.Month, 1),
                PowerPeak = x.Skip(1).Select(y => y.MonthlyPowerPeak).Max(),
            })
            .ToListAsync();

        var averagePowerPeak = entries.Any() ? entries.Average(x => Math.Max(x.PowerPeak, 2.5M)) : 2.5M;

        var capacityCost =
              importToday * electricityFlag.GreenEnergyContribution / 100M // Bijdrage groene stroom: 1.554 c€/kWh
            + electricityFlag.FixedCharge / (daily ? 365M : 12M) // Vaste vergoeding: 42.4 €/jaar
            + electricityFlag.CapacityTariff * averagePowerPeak / (daily ? 365M : 12M) // Capaciteitstarief: 53.2565412 €/KW/jaar (min 2.5 kW)
            + importToday * electricityFlag.UsageTariff / 100M // Afnametarief:  5.99007 c€/kWh
            + electricityFlag.DataAdministration / (daily ? 365M : 12M) // Tarief databeheer: 18.56 €/jaar
            + importToday * electricityFlag.SpecialExciseTax / 100M // Bijzondere accijns: 5.03288 c€/kWh
            + importToday * electricityFlag.EnergyContribution / 100M; // Energiebijdrage: 0.20417 c€/kWh

        return energyCost + capacityCost;
    }
}