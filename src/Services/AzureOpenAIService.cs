using Azure;
using Azure.AI.OpenAI;
using BriefingTool.Services.Interfaces;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Chat;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace BriefingTool.Services;

public class AzureOpenAIService : IAzureOpenAIService
{
    public ChatCompletionOptions CreateChatCompletionOptions()
    {
        return new ChatCompletionOptions
        {
            Temperature = (float)0.7,
            MaxOutputTokenCount = 6553,

            TopP = (float)0.95,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
        };
    }
    public ChatClient GetChatClient(string azureOpenaiKey, string azureOpenaiEndpoint, string azureOpenaiDeployment)
    {
        var azureClient = InitialiseAzureOpenAIClient(azureOpenaiKey, azureOpenaiEndpoint);
         
        return azureClient.GetChatClient(azureOpenaiDeployment);
    }

    public AzureOpenAIClient InitialiseAzureOpenAIClient(string azureOpenaiKey, string azureOpenaiEndpoint)
    {
        if (string.IsNullOrEmpty(azureOpenaiKey)) 
            throw new ArgumentNullException(nameof(azureOpenaiKey), "OpenAI API Key not found"); 

        if (string.IsNullOrEmpty(azureOpenaiEndpoint)) 
            throw new ArgumentNullException(nameof(azureOpenaiEndpoint), "OpenAI Endpoint not found"); 

        if (!Uri.TryCreate(azureOpenaiEndpoint, UriKind.Absolute, out var endpointUri)) 
            throw new ArgumentException("Invalid Azure OpenAI Endpoint URI", nameof(azureOpenaiEndpoint));

        var azureOpenAIClientOptions = new AzureOpenAIClientOptions()
        {
            MessageLoggingPolicy = new MessageLoggingPolicy(
                new ClientLoggingOptions()
                {
                    EnableLogging = true,
                    EnableMessageContentLogging = true
                })
        };
        return new AzureOpenAIClient(endpointUri, new AzureKeyCredential(azureOpenaiKey), azureOpenAIClientOptions);
    }
    public OpenAIClient InitialiseOpenAIClient(string azureOpenaiKey, string azureOpenaiEndpoint)
    {
        if (string.IsNullOrEmpty(azureOpenaiKey))
            throw new ArgumentNullException(nameof(azureOpenaiKey), "OpenAI API Key not found");

        if (string.IsNullOrEmpty(azureOpenaiEndpoint))
            throw new ArgumentNullException(nameof(azureOpenaiEndpoint), "OpenAI Endpoint not found");

        if (!Uri.TryCreate(azureOpenaiEndpoint, UriKind.Absolute, out var endpointUri))
            throw new ArgumentException("Invalid Azure OpenAI Endpoint URI", nameof(azureOpenaiEndpoint));

        var clientOptions = new OpenAIClientOptions() { Endpoint = endpointUri };
         
        return new OpenAIClient(new ApiKeyCredential(azureOpenaiKey), clientOptions);
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
    private static AzureOpenAIClientOptions.ServiceVersion GetServiceVersion(string? apiVersion)
    {
        return apiVersion switch
        {
            "2024-10-21" => AzureOpenAIClientOptions.ServiceVersion.V2024_10_21,
            "2024-06-01" => AzureOpenAIClientOptions.ServiceVersion.V2024_06_01,
            _ => AzureOpenAIClientOptions.ServiceVersion.V2024_10_21
        };
    }
}
