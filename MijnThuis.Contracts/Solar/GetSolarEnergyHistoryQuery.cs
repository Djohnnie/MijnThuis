using MediatR;

namespace MijnThuis.Contracts.Solar;

public class GetSolarEnergyHistoryQuery : IRequest<GetSolarEnergyHistoryResponse>
{
    public EnergyHistoryUnit Unit { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

public enum EnergyHistoryUnit
{
    Day,
    Month,
    Year
}