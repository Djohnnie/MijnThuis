using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MijnThuis.Dashboard.Web.Copilot;
using MijnThuis.Dashboard.Web.Notifications;
using MudBlazor;

namespace MijnThuis.Dashboard.Web.Components;

public partial class CopilotTile
{
    [Inject] private SpeechToTextNotificationService _sttService { get; set; }

    private MudTextField<string> InputFieldRef { get; set; }
    public string Prompt { get; set; }
    public string TTSKey { get; set; }
    public string TTSRegion { get; set; }
    public string TTSLanguage { get; set; }

    protected override void OnInitialized()
    {
        _sttService.EventClick += _sttService_EventClick;
        var config = ScopedServices.GetRequiredService<IConfiguration>();
        TTSKey = config.GetValue<string>("SPEECH_API_KEY");
        TTSRegion = config.GetValue<string>("SPEECH_REGION");
        TTSLanguage = config.GetValue<string>("SPEECH_LANGUAGE");
    }

    private async void _sttService_EventClick(object? sender, EventArgs e)
    {
        var result = await JS.InvokeAsync<string>("executeTextToSpeech", TTSLanguage, TTSKey, TTSRegion);
        Prompt = result;

        StateHasChanged();

        await ExecutePrompt();
    }

    public async Task ExecutePrompt()
    {
        var copilotHelper = ScopedServices.GetRequiredService<ICopilotHelper>();

        var prompt = Prompt;
        Prompt = $"De MijnThuis Copilot is aan het nadenken over uw vraag '{prompt}'...";
        StateHasChanged();

        Prompt = await copilotHelper.ExecutePrompt(prompt);

        StateHasChanged();
    }

    public async void ExecutePromptOnKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            await InputFieldRef.BlurAsync();
            await Task.Delay(100);
            StateHasChanged();
            Prompt = Prompt.Trim();
            StateHasChanged();
            await ExecutePrompt();
            await InputFieldRef!.FocusAsync();
        }
    }
}