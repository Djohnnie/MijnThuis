using MediatR;

namespace MijnThuis.Contracts.Heating;

public class GetInvoicedGasCostQuery : IRequest<GetInvoicedGasCostResponse>
{
    public int Year { get; set; }
}