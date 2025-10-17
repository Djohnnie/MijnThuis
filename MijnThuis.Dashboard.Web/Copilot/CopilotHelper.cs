using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MijnThuis.Application.DependencyInjection;
using OpenAI;
using System.ClientModel;
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
        var instructionBuilder = new StringBuilder();
        instructionBuilder.Append("You are a digital assistent that can answer questions about the different home automation tools.");
        instructionBuilder.Append("If your answer contains a decimal number, always show 1 digit after the decimal point.");

        var agent = InitializeAgent("MijnThuis", "MijnThuis Agent", instructionBuilder.ToString());

        var agentThread = agent.GetNewThread();

        var response = await agent.RunAsync(new ChatMessage(ChatRole.User, prompt), agentThread);

        return response.ToString();
    }

    private AIAgent InitializeAgent(string name, string description, string instructions)
    {
        var deploymentName = _configuration.GetValue<string>("AZURE_OPEN_AI_DEPLOYMENT_NAME");
        var endpoint = _configuration.GetValue<string>("AZURE_OPEN_AI_ENDPOINT");
        var apiKey = _configuration.GetValue<string>("AZURE_OPEN_AI_APIKEY");

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddApplication();
        serviceCollection.AddMemoryCache();
        serviceCollection.AddSingleton(_configuration);

        var tools =
            MijnThuisCopilotGeneralFunctions.GetTools().Concat(
                MijnThuisCopilotSolarFunctions.GetTools().Concat(
                    MijnThuisCopilotPowerFunctions.GetTools().Concat(
                        MijnThuisCopilotCarFunctions.GetTools().Concat(
                            MijnThuisCopilotHeatingFunctions.GetTools().Concat(
                                MijnThuisCopilotSaunaFunctions.GetTools()))))).ToList();

        var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
        var chatClient = client.GetChatClient(deploymentName);
        var agentClient = chatClient.CreateAIAgent(
            name: name, description: description, instructions: instructions, tools: tools, services: serviceCollection.BuildServiceProvider());
        return agentClient;
    }
}