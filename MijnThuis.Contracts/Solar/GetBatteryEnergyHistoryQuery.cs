using MediatR;

namespace MijnThuis.Contracts.Solar;

public class GetBatteryEnergyHistoryQuery : IRequest<GetBatteryEnergyHistoryResponse>
{
    public EnergyHistoryUnit Unit { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}