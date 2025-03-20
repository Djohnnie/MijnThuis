using MediatR;

namespace MijnThuis.Contracts.Car;

public class SetManualCarChargeCommand : IRequest<SetManualCarChargeResponse>
{
    public bool IsEnabled { get; set; }
    public int ChargeAmps { get; set; }
}