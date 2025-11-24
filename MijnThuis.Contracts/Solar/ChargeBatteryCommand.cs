using MediatR;

namespace MijnThuis.Contracts.Solar;

public class ChargeBatteryCommand : IRequest<ChargeBatteryResponse>
{
    public TimeSpan Duration { get; set; }
    public int Power { get; set; }
}