using Microsoft.JSInterop;
using MijnThuis.Dashboard.Web.Copilot;

namespace MijnThuis.Dashboard.Web.Components;

public partial class CopilotTile
{
    public string Prompt { get; set; }
    public string TTSKey { get; set; }
    public string TTSRegion { get; set; }
    public string TTSLanguage { get; set; }

    protected override void OnInitialized()
    {
        var config = ScopedServices.GetRequiredService<IConfiguration>();
        TTSKey = config.GetValue<string>("SPEECH_API_KEY");
        TTSRegion = config.GetValue<string>("SPEECH_REGION");
        TTSLanguage = config.GetValue<string>("SPEECH_LANGUAGE");
    }

    public async Task ExecutePrompt()
    {
        var copilotHelper = ScopedServices.GetRequiredService<ICopilotHelper>();

        var prompt = Prompt;
        Prompt = "De MijnThuis Copilot is aan het nadenken...";
        Prompt = await copilotHelper.ExecutePrompt(prompt);

        StateHasChanged();
    }

    public async Task ExecuteTTS()
    {
        var result = await JS.InvokeAsync<string>("executeTextToSpeech", TTSLanguage, TTSKey, TTSRegion);
        Prompt = result;

        StateHasChanged();
    }
}