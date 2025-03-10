using MediatR;
using MijnThuis.Contracts.Solar;

namespace MijnThuis.Contracts.Car;

public class GetCarChargingHistoryQuery : IRequest<GetCarChargingHistoryResponse>
{
    public EnergyHistoryUnit Unit { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}