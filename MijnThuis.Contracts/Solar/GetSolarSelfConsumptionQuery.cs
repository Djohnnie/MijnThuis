using MediatR;

namespace MijnThuis.Contracts.Solar;

public class GetSolarSelfConsumptionQuery : IRequest<GetSolarSelfConsumptionResponse>
{
    public DateTime Date { get; set; }
}