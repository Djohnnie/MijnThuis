using MediatR;
using MijnThuis.Contracts.SmartLock;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace MijnThuis.Dashboard.Web.Tools;

[McpServerToolType]
public class MijnThuisSmartLockTools
{
    private readonly IMediator _mediator;

    public MijnThuisSmartLockTools(IMediator mediator)
    {
        _mediator = mediator;
    }

    [McpServerTool(Name = $"mijnthuis_smartlock_{nameof(GetSmartLockInformation)}", ReadOnly = true)]
    [Description("Gets information about my smart lock at home like state of the lock, state of the door and smart lock battery percentage.")]
    [return: Description("Information, formatted in JSON containing the state of the lock, state of the door and smart lock battery percentage.")]
    public async Task<string> GetSmartLockInformation()
    {
        var lockInfo = await _mediator.Send(new GetSmartLockOverviewQuery());
        return JsonSerializer.Serialize(lockInfo);
    }
}