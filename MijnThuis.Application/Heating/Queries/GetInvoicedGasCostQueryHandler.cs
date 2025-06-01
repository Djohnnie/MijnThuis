using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Heating;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Heating.Queries;

public class GetInvoicedGasCostQueryHandler : IRequestHandler<GetInvoicedGasCostQuery, GetInvoicedGasCostResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetInvoicedGasCostQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetInvoicedGasCostResponse> Handle(GetInvoicedGasCostQuery request, CancellationToken cancellationToken)
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

    private async Task<GetInvoicedGasCostResponse> HandleForYear(int year, int lastYear, CancellationToken cancellationToken)
    {
        var invoices = await _dbContext.EnergyInvoices
            .Where(x => x.Date.Year == year || x.Date.Year == lastYear)
            .ToListAsync(cancellationToken);

        var result = new GetInvoicedGasCostResponse
        {
            ThisYear = new List<GasInvoiceEntry>(12),
            LastYear = new List<GasInvoiceEntry>(12)
        };

        for (int month = 1; month <= 12; month++)
        {
            result.ThisYear.Add(new GasInvoiceEntry
            {
                Date = new DateTime(year, month, 1),
                GasAmount = invoices
                    .SingleOrDefault(x => x.Date.Year == year && x.Date.Month == month)?.GasAmount ?? 0M
            });
            result.LastYear.Add(new GasInvoiceEntry
            {
                Date = new DateTime(lastYear, month, 1),
                GasAmount = invoices
                    .SingleOrDefault(x => x.Date.Year == lastYear && x.Date.Month == month)?.GasAmount ?? 0M
            });
        }

        return result;
    }

    private async Task<GetInvoicedGasCostResponse> HandleForAllYears(CancellationToken cancellationToken)
    {
        var years = await _dbContext.EnergyInvoices
            .OrderBy(x => x.Date.Year)
            .GroupBy(x => x.Date.Year)
            .Select(x => new GasInvoiceEntry { Date = new DateTime(x.Key, 1, 1), GasAmount = x.Sum(x => x.GasAmount) })
            .ToListAsync(cancellationToken);

        return new GetInvoicedGasCostResponse { AllYears = years };
    }
}