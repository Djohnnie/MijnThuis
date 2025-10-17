using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace MijnThuis.Dashboard.Web.Copilot;

public class MijnThuisCopilotGeneralFunctions
{
    public static IList<AITool> GetTools()
    {
        return [
            AIFunctionFactory.Create(GetTime),
            AIFunctionFactory.Create(GetDate),
            AIFunctionFactory.Create(GetName)
        ];
    }

    [Description("Gets the current time.")]
    public static async Task<TimeSpan> GetTime()
    {
        return TimeProvider.System.GetLocalNow().TimeOfDay;
    }

    [Description("Gets the current date.")]
    public static async Task<DateTime> GetDate()
    {
        return TimeProvider.System.GetLocalNow().Date;
    }

    [Description("Gets your name.")]
    public static async Task<string> GetName()
    {
        return "MijnThuis Copilot";
    }
}