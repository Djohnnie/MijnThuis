using MediatR;

namespace MijnThuis.Contracts.Power;

public class GetEnergyUsageQuery : IRequest<GetEnergyUsageResponse>
{
    public DateTime Date { get; set; }
    public PowerUsageUnit Unit { get; set; }
}

public enum PowerUsageUnit
{
    Day,
    Month,
    Year
}