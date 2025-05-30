using MediatR;

namespace MijnThuis.Contracts.Power;

public class GetInvoicedPowerEnergyCostQuery : IRequest<GetInvoicedPowerEnergyCostResponse>
{
    public int Year { get; set; }
}