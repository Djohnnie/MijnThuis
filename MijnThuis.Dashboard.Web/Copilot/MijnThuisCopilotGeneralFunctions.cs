using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace MijnThuis.Dashboard.Web.Copilot;

public class MijnThuisCopilotGeneralFunctions
{
    [KernelFunction]
    [Description("Gets the current time.")]
    public async Task<TimeSpan> GetTime()
    {
        return TimeProvider.System.GetLocalNow().TimeOfDay;
    }

    [KernelFunction]
    [Description("Gets the current date.")]
    public async Task<DateTime> GetDate()
    {
        return TimeProvider.System.GetLocalNow().Date;
    }

    [KernelFunction]
    [Description("Gets your name.")]
    public async Task<string> GetName()
    {
        return "MijnThuis Copilot";
    }
}