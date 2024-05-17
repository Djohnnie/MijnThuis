using MijnThuis.Dashboard.Web.Copilot;

namespace MijnThuis.Dashboard.Web.Components;

public partial class CopilotTile
{
    public string Prompt { get; set; }

    public async Task ExecutePrompt()
    {
        var copilotHelper = ScopedServices.GetRequiredService<ICopilotHelper>();

        var prompt = Prompt;
        Prompt = "De MijnThuis Copilot is aan het nadenken...";
        Prompt = await copilotHelper.ExecutePrompt(prompt);

        StateHasChanged();
    }
}