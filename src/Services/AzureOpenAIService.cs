using Azure;
using Azure.AI.OpenAI;
using BriefingTool.Services.Interfaces;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Chat;
using System.Diagnostics.CodeAnalysis;

namespace BriefingTool.Services;

public class AzureOpenAIService : IAzureOpenAIService
{
    public ChatCompletionOptions CreateChatCompletionOptions()
    {
        return new ChatCompletionOptions();
    }
    public ChatClient GetChatClient(string azureOpenaiKey, string azureOpenaiEndpoint, string azureOpenaiDeployment)
    {
        var azureClient = InitialiseAzureOpenAIClient(azureOpenaiKey, azureOpenaiEndpoint);
         
        return azureClient.GetChatClient(azureOpenaiDeployment);
    }

    public AzureOpenAIClient InitialiseAzureOpenAIClient(string azureOpenaiKey, string azureOpenaiEndpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(azureOpenaiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(azureOpenaiEndpoint);

        if (!Uri.TryCreate(azureOpenaiEndpoint,UriKind.Absolute, out var endpointUri))
        {
            throw new ArgumentException("Invalid Azure OpenAI Endpoint URI", nameof(azureOpenaiEndpoint));
        }

        var options = new AzureOpenAIClientOptions(AzureOpenAIClientOptions.ServiceVersion.V2024_10_21);

        return new AzureOpenAIClient(endpointUri, new AzureKeyCredential(azureOpenaiKey), options);
    }

    [Experimental("AOAI002")]
    public async Task<Assistant> CreateAgentAssistantAsync(OpenAIClient openAIClient, string model, string instruction)
    {
        ArgumentNullException.ThrowIfNull(openAIClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(instruction);

        AssistantClient assistantClient = openAIClient.GetAssistantClient();

        var options = new AssistantCreationOptions
        {
            Name = $"BriefingAgent-{Guid.NewGuid()}",
            Instructions = instruction,
        };

        Assistant agent = await assistantClient.CreateAssistantAsync(model, options);

        return agent;
    }
}
