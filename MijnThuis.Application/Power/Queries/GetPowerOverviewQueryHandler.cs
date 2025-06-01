using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly IShellyService _shellyService;
    private readonly ISolarService _solarService;
    private readonly IModbusService _modbusService;
    private readonly IDayAheadEnergyPricesRepository _energyPricesRepository;
    private readonly IMemoryCache _memoryCache;

    public GetPowerOverviewQueryHandler(
        MijnThuisDbContext dbContext,
        IPowerService powerService,
        IShellyService shellyService,
        ISolarService solarService,
        IModbusService modbusService,
        IDayAheadEnergyPricesRepository energyPricesRepository,
        IMemoryCache memoryCache)
    {
        _dbContext = dbContext;
        _powerService = powerService;
        _shellyService = shellyService;
        _solarService = solarService;
        _modbusService = modbusService;
        _energyPricesRepository = energyPricesRepository;
        _memoryCache = memoryCache;
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
        var energyPricing = await _energyPricesRepository.GetEnergyPriceForTimestamp(DateTime.Now);
        var tvPowerSwitchOverview = await _shellyService.GetTvPowerSwitchOverview();
        var bureauPowerSwitchOverview = await _shellyService.GetBureauPowerSwitchOverview();
        var vijverPowerSwitchOverview = await _shellyService.GetVijverPowerSwitchOverview();

        var result = powerResult.Adapt<GetPowerOverviewResponse>();
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

        return result;
    }
}