using MediatR;
using MijnThuis.Contracts.Solar;

namespace MijnThuis.Contracts.Power;

public class GetEnergyHistoryQuery : IRequest<GetEnergyHistoryResponse>
{
    public EnergyHistoryUnit Unit { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}