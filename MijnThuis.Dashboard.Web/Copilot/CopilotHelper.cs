using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MijnThuis.Application.DependencyInjection;

namespace MijnThuis.Dashboard.Web.Copilot;

public interface ICopilotHelper
{
    Task<string> ExecutePrompt(string prompt);
}

public class CopilotHelper : ICopilotHelper
{
    private readonly IConfiguration _configuration;

    public CopilotHelper(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> ExecutePrompt(string prompt)
    {
        var kernel = InitializeSemanticKernel();
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        ChatHistory chatHistory = [];
        chatHistory.AddSystemMessage("Please answer in only one sentence.");
        chatHistory.AddSystemMessage("If you don't have an answer to a question, please excuse yourself and explain you don't have an answer.");
        chatHistory.AddUserMessage(prompt);
        var response = await chatCompletionService.GetChatMessageContentAsync(chatHistory, executionSettings, kernel);

        return response.ToString();
    }

    private Kernel InitializeSemanticKernel()
    {
        var builder = Kernel.CreateBuilder();

        var deploymentName = _configuration.GetValue<string>("AZURE_OPEN_AI_DEPLOYMENT_NAME");
        var endpoint = _configuration.GetValue<string>("AZURE_OPEN_AI_ENDPOINT");
        var apiKey = _configuration.GetValue<string>("AZURE_OPEN_AI_APIKEY");

        builder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
        builder.Services.AddApplication();
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton(_configuration);

        builder.Plugins.AddFromType<MijnThuisCopilotGeneralFunctions>();
        builder.Plugins.AddFromType<MijnThuisCopilotSolarFunctions>();
        builder.Plugins.AddFromType<MijnThuisCopilotPowerFunctions>();
        builder.Plugins.AddFromType<MijnThuisCopilotCarFunctions>();
        builder.Plugins.AddFromType<MijnThuisCopilotHeatingFunctions>();
        builder.Plugins.AddFromType<MijnThuisCopilotSaunaFunctions>();

        var kernel = builder.Build();

        return kernel;
    }
}