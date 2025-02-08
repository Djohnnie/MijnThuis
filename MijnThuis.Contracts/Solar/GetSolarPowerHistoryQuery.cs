using MediatR;

namespace MijnThuis.Contracts.Solar;

public class GetSolarPowerHistoryQuery : IRequest<GetSolarPowerHistoryResponse>
{
    public PowerHistoryUnit Unit { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

public enum PowerHistoryUnit
{
    FifteenMinutes,
    Day,
    Month,
    Year
}