using MediatR;
using MijnThuis.Contracts.SmartLock;
using MijnThuis.Integrations.SmartLock;

namespace MijnThuis.Application.SmartLock.Commands;

public class UnlockSmartLockCommandHandler : IRequestHandler<UnlockSmartLockCommand, SmartLockResponse>
{
    private readonly ISmartLockService _smartLockService;

    public UnlockSmartLockCommandHandler(
        ISmartLockService smartLockService)
    {
        _smartLockService = smartLockService;
    }

    public async Task<SmartLockResponse> Handle(UnlockSmartLockCommand request, CancellationToken cancellationToken)
    {
        await _smartLockService.Unlock();

        var isUnlocked = false;

        while (!isUnlocked)
        {
            var overview = await _smartLockService.GetOverview();
            isUnlocked = overview.State == SmartLockState.Unlocked;

            if (!isUnlocked)
            {
                await Task.Delay(500);
            }
        }

        return new SmartLockResponse();
    }
}