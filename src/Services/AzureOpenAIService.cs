using Azure;
using Azure.AI.OpenAI; 
using BriefingTool.Services.Interfaces;
using OpenAI.Chat;
using System.ClientModel.Primitives;

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
        AzureOpenAIClient azureClient = InitialiseAzureOpenAIClient(azureOpenaiKey, azureOpenaiEndpoint);
         
        return azureClient.GetChatClient(azureOpenaiDeployment);
    }

    public AzureOpenAIClient InitialiseAzureOpenAIClient(string azureOpenaiKey, string azureOpenaiEndpoint, bool excludeAzureOpenAIClientOptions = false)
    {
        if (string.IsNullOrEmpty(azureOpenaiKey)) 
            throw new ArgumentNullException(nameof(azureOpenaiKey), "OpenAI API Key not found"); 

        if (string.IsNullOrEmpty(azureOpenaiEndpoint)) 
            throw new ArgumentNullException(nameof(azureOpenaiEndpoint), "OpenAI Endpoint not found"); 

        if (!Uri.TryCreate(azureOpenaiEndpoint, UriKind.Absolute, out var endpointUri)) 
            throw new ArgumentException("Invalid Azure OpenAI Endpoint URI", nameof(azureOpenaiEndpoint));

        var azureOpenAIClientOptions = excludeAzureOpenAIClientOptions ? null : new AzureOpenAIClientOptions()
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
}
