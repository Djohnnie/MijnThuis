using MediatR;

namespace MijnThuis.Contracts.Solar;

public class GetSolarForecastHistoryQuery : IRequest<GetSolarForecastHistoryResponse>
{
    public DateTime Date { get; set; }
}