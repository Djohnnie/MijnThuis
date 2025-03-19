using MediatR;

namespace MijnThuis.Contracts.SmartLock;

public class UnlockSmartLockCommand : IRequest<SmartLockResponse>
{
}