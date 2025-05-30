using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Power;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Power.Queries;

public class GetInvoicedPowerEnergyCostQueryHandler : IRequestHandler<GetInvoicedPowerEnergyCostQuery, GetInvoicedPowerEnergyCostResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetInvoicedPowerEnergyCostQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetInvoicedPowerEnergyCostResponse> Handle(GetInvoicedPowerEnergyCostQuery request, CancellationToken cancellationToken)
    {
        if (request.Year == 0)
        {
            return await HandleForAllYears(cancellationToken);
        }
        else
        {
            return await HandleForYear(request.Year, request.Year - 1, cancellationToken);
        }
    }

    private async Task<GetInvoicedPowerEnergyCostResponse> HandleForYear(int year, int lastYear, CancellationToken cancellationToken)
    {
        var invoices = await _dbContext.EnergyInvoices
            .Where(x => x.Date.Year == year || x.Date.Year == lastYear)
            .ToListAsync(cancellationToken);

        var result = new GetInvoicedPowerEnergyCostResponse
        {
            ThisYear = new List<EnergyInvoiceEntry>(12),
            LastYear = new List<EnergyInvoiceEntry>(12)
        };

        for (int month = 1; month <= 12; month++)
        {
            result.ThisYear.Add(new EnergyInvoiceEntry
            {
                Date = new DateTime(year, month, 1),
                ElectricityAmount = invoices
                    .SingleOrDefault(x => x.Date.Year == year && x.Date.Month == month)?.ElectricityAmount ?? 0M
            });
            result.LastYear.Add(new EnergyInvoiceEntry
            {
                Date = new DateTime(lastYear, month, 1),
                ElectricityAmount = invoices
                    .SingleOrDefault(x => x.Date.Year == lastYear && x.Date.Month == month)?.ElectricityAmount ?? 0M
            });
        }

        return result;
    }

    private async Task<GetInvoicedPowerEnergyCostResponse> HandleForAllYears(CancellationToken cancellationToken)
    {
        var years = await _dbContext.EnergyInvoices
            .OrderBy(x => x.Date.Year)
            .GroupBy(x => x.Date.Year)
            .Select(x => new EnergyInvoiceEntry { Date = new DateTime(x.Key, 1, 1), ElectricityAmount = x.Sum(x => x.ElectricityAmount) })
            .ToListAsync(cancellationToken);

        return new GetInvoicedPowerEnergyCostResponse { AllYears = years };
    }
}