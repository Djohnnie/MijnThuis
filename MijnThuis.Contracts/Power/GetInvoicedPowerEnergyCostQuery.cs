using MediatR;

namespace MijnThuis.Contracts.Power;

public class GetInvoicedEnergyCostQuery : IRequest<GetInvoicedEnergyCostResponse>
{
    public int Year { get; set; }
}