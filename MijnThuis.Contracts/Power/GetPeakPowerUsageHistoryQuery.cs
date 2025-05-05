using MediatR;

namespace MijnThuis.Contracts.Power;

public class GetPeakPowerUsageHistoryQuery : IRequest<GetPeakPowerUsageHistoryResponse>
{
    public int Year { get; set; }
}