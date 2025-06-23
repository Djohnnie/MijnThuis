using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Power;
using MijnThuis.DataAccess;
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

    public GetPowerOverviewQueryHandler(
        MijnThuisDbContext dbContext,
        IPowerService powerService,
        IShellyService shellyService,
        IModbusService modbusService,
        ISamsungService samsungService,
        IDayAheadEnergyPricesRepository energyPricesRepository)
    {
        _dbContext = dbContext;
        _powerService = powerService;
        _shellyService = shellyService;
        _modbusService = modbusService;
        _samsungService = samsungService;
        _energyPricesRepository = energyPricesRepository;
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
        var energyThisMonth = await _dbContext.EnergyHistory
            .Where(x => x.Date.Date >= new DateTime(today.Year, today.Month, 1) && x.Date.Date <= today)
            .Select(x => new
            {
                ImportToday = x.TotalImportDelta,
                ExportToday = x.TotalExportDelta
            }).ToListAsync();
        var negativePriceRange = await _energyPricesRepository.GetNegativeInjectionPriceRange();
        var energyPricing = await _energyPricesRepository.GetEnergyPriceForTimestamp(DateTime.Now);
        var tvPowerSwitchOverview = await _shellyService.GetTvPowerSwitchOverview();
        var bureauPowerSwitchOverview = await _shellyService.GetBureauPowerSwitchOverview();
        var vijverPowerSwitchOverview = await _shellyService.GetVijverPowerSwitchOverview();

        var result = powerResult.Adapt<GetPowerOverviewResponse>();
        result.Description = $"Negatief injectietarief {(negativePriceRange.From.Date == DateTime.Today ? "vandaag" : "morgen")} tussen {negativePriceRange.From.Hour}u en {negativePriceRange.To.Hour}u";
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

        return result;
    }
}