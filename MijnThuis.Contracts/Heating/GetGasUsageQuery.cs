using MediatR;

namespace MijnThuis.Contracts.Heating;

public class GetGasUsageQuery : IRequest<GetGasUsageResponse>
{
    public DateTime Date { get; set; }
    public GasUsageUnit Unit { get; set; }
}

public enum GasUsageUnit
{
    Day,
    Month,
    Year
}