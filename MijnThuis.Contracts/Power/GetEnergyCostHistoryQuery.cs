using MediatR;
using MijnThuis.Contracts.Solar;

namespace MijnThuis.Contracts.Power;

public class GetEnergyCostHistoryQuery : IRequest<GetEnergyCostHistoryResponse>
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public EnergyHistoryUnit Unit { get; set; }
}