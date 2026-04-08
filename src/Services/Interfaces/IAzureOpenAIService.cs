using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace BriefingTool.Services.Interfaces;

public interface IAzureOpenAIService
{
    /// <summary>
    /// Gets the ChatClient with the specified deployment name
    /// </summary>
    /// <param name="azureOpenaiKey">A key of Azure Open AI.</param>
    /// <param name="azureOpenaiEndpoint">An endpoint of Azure Open AI.</param>
    /// <param name="azureOpenaiDeployment">A deployment of Azure Open AI.</param>
    /// <returns></returns>
    ChatClient GetChatClient(string azureOpenaiKey, string azureOpenaiEndpoint, string azureOpenaiDeployment);

    /// <summary>
    /// Create chat completion options 
    /// </summary>
    /// <returns></returns>
    ChatCompletionOptions CreateChatCompletionOptions();

    /// <summary>
    /// Initialise Azure Open API client.
    /// </summary>
    /// <param name="azureOpenaiKey">A key of Azure Open AI.</param>
    /// <param name="azureOpenaiEndpoint">An endpoint of Azure Open AI.</param>
    /// <param name="excludeAzureOpenAIClientOptions">Exclude Azure Open AI client options (By default false).</param>
    /// <returns></returns>
    AzureOpenAIClient InitialiseAzureOpenAIClient(string azureOpenaiKey, string azureOpenaiEndpoint, bool excludeAzureOpenAIClientOptions = false);
}
