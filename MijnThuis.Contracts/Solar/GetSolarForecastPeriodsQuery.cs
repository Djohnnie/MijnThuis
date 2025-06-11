using MediatR;

namespace MijnThuis.Contracts.Solar;

public class GetSolarForecastPeriodsQuery : IRequest<GetSolarForecastPeriodsResponse>
{
    public DateTime Date { get; set; }
}