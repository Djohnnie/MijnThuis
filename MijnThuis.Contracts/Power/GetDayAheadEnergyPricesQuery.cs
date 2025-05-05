using MediatR;

namespace MijnThuis.Contracts.Power;

public class GetDayAheadEnergyPricesQuery : IRequest<GetDayAheadEnergyPricesResponse>
{
    public DateTime Date { get; set; }
}