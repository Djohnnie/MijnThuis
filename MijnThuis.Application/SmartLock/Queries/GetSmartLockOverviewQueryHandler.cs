using MediatR;
using MijnThuis.Contracts.SmartLock;
using MijnThuis.Integrations.SmartLock;

namespace MijnThuis.Application.SmartLock.Queries;

public class GetSmartLockOverviewQueryHandler : IRequestHandler<GetSmartLockOverviewQuery, GetSmartLockOverviewResponse>
{
    private readonly ISmartLockService _smartLockService;

    public GetSmartLockOverviewQueryHandler(
        ISmartLockService smartLockService)
    {
        _smartLockService = smartLockService;
    }

    public async Task<GetSmartLockOverviewResponse> Handle(GetSmartLockOverviewQuery request, CancellationToken cancellationToken)
    {
        var overview = await _smartLockService.GetOverview();
        var activityLog = await _smartLockService.GetActivityLog();

        return new GetSmartLockOverviewResponse
        {
            State = Map(overview.State),
            DoorState = Map(overview.DoorState),
            BatteryCharge = overview.BatteryCharge,
            ActivityLog = Map(activityLog)
        };
    }

    private string Map(SmartLockState state)
    {
        return state switch
        {
            SmartLockState.Locked => "Op slot",
            SmartLockState.Unlocking => "Van het slot aan het halen",
            SmartLockState.Unlocked => "Niet op slot",
            SmartLockState.Locking => "Op slot aan het doen",
            _ => "Onbekend"
        };
    }

    private string Map(SmartLockDoorState doorState)
    {
        return doorState switch
        {
            SmartLockDoorState.DoorOpened => "Deur staat open",
            SmartLockDoorState.DoorClosed => "Deur is gesloten",
            _ => "Onbekend"
        };
    }

    private List<SmartLockActivityLogEntry> Map(List<SmartLockLog> activityLog)
    {
        return activityLog.Select(x => new SmartLockActivityLogEntry
        {
            Timestamp = x.Timestamp,
            Action = x.Action switch
            {
                SmartLockAction.Unlock => "Ontgrendeld",
                SmartLockAction.Lock => "Op slot",
                SmartLockAction.Unlatch => "Ontgrendeld",
                SmartLockAction.DoorOpened => "Deur geopend",
                SmartLockAction.DoorClosed => "Deur gesloten",
                _ => "Onbekend"
            }
        }).ToList();
    }
}