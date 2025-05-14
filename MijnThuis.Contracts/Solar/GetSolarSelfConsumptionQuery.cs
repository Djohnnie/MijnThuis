using MediatR;

namespace MijnThuis.Contracts.Solar;

public class GetSolarSelfConsumptionQuery : IRequest<GetSolarSelfConsumptionResponse>
{
    public DateTime Date { get; set; }

    public bool ShouldIncludeEntries { get; set; }

    public SolarSelfConsumptionRange Range { get; set; } = SolarSelfConsumptionRange.Day;
}

public enum SolarSelfConsumptionRange
{
    Day,
    Month,
    Year
}