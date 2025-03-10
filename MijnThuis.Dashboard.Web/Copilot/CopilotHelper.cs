using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MijnThuis.Application.DependencyInjection;
using System.Text;

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
        kernel.ImportPluginFromType<MijnThuisCopilotGeneralFunctions>();
        kernel.ImportPluginFromType<MijnThuisCopilotSolarFunctions>();
        kernel.ImportPluginFromType<MijnThuisCopilotPowerFunctions>();
        kernel.ImportPluginFromType<MijnThuisCopilotCarFunctions>();
        kernel.ImportPluginFromType<MijnThuisCopilotHeatingFunctions>();
        kernel.ImportPluginFromType<MijnThuisCopilotSaunaFunctions>();

        var instructionBuilder = new StringBuilder();
        instructionBuilder.Append("You are a digital assistent that can answer questions about the different home automation tools.");
        instructionBuilder.Append("If your answer contains a decimal number, always show 1 digit after the decimal point.");

        var agent = new ChatCompletionAgent
        {
            Name = "MijnThuis",
            Instructions = instructionBuilder.ToString(),
            Kernel = kernel,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            })
        };

        ChatHistory chatHistory = [];
        chatHistory.AddUserMessage(prompt);

        var responseBuilder = new StringBuilder();

        await foreach (var response in agent.InvokeAsync(chatHistory))
        {
            responseBuilder.Append(response.ToString());
        }

        return responseBuilder.ToString();
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

        var kernel = builder.Build();

        return kernel;
    }
}