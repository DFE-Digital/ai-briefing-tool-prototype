using Azure.AI.OpenAI.Chat;
using BriefingTool.Builders.Interfaces;
using BriefingTool.Config;
using BriefingTool.Models;
using BriefingTool.Retrievers.Interfaces;
using BriefingTool.Runners.Interfaces;
using BriefingTool.Services.Interfaces;
using Markdig;
using OpenAI.Chat;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BriefingTool.Runners;

public class SingleSourceBriefingRunner(ILogger<SingleSourceBriefingRunner> logger,
    IBasePromptRetriever basePromptRetriever,
    IConcernsPromptRetriever concernsPromptRetriever,
    IOfstedPromptRetriever ofstedPromptRetriever,
    IOfstedSummaryPromptRetriever ofstedSummaryPromptRetriever,
    IConcernsInformationRetriever concernsInformationRetriever,
    AzureSettings azureSettings,
    IAzureOpenAIService azureOpenAIService,
    IPromptBuilder promptBuilder) : IBriefingRunner
{
    private const string OfstedIndexName = "ofstedindex";

    [Experimental("AOAI001")]
    public async Task<AIResult> GetBriefing(BriefingParameters briefing)
    {
        if (string.IsNullOrEmpty(briefing.AcademyName))
        {
            return new AIResult("", "Enter an academy name", -1);
        }
        logger.LogInformation("AI endpoint: {Endpoint}", azureSettings.AzureOpenaiEndpoint);

        var chatClient = azureOpenAIService.GetChatClient(azureSettings.AzureOpenaiKey, azureSettings.AzureOpenaiEndpoint, azureSettings.AzureOpenaiDeployment);
        promptBuilder.AddSystemMessage(basePromptRetriever.GetPrompt());

        var chatCompletionOptions = azureOpenAIService.CreateChatCompletionOptions();


        // AI Search data source
        AzureSearchChatDataSource GetChatDataSource(string azureOpenaiKey, string azureOpenaiEndpoint, string indexName)
        {
            return new AzureSearchChatDataSource()
            {
                Endpoint = new Uri(azureOpenaiEndpoint),
                IndexName = indexName,
                Authentication = DataSourceAuthentication.FromApiKey(azureOpenaiKey),
                MaxSearchQueries = 1
            };
        }
        void SetAISearchDataSourceForOfsted(BriefingParameters briefing, ChatCompletionOptions chatCompletionOptions, AzureSettings azureSettings)
        {
            // AI Search data source
            if (briefing.Ofsted)
            {
                chatCompletionOptions.AddDataSource(GetChatDataSource(azureSettings.AzureSearchKey, azureSettings.AzureSearchEndpoint, OfstedIndexName));

                promptBuilder.AddUserMessage(ofstedPromptRetriever.GetPrompt());
            }
            if (briefing.OfstedSummary)
            {
                if (!briefing.Ofsted)
                    chatCompletionOptions.AddDataSource(GetChatDataSource(azureSettings.AzureSearchKey, azureSettings.AzureSearchEndpoint, OfstedIndexName));

                promptBuilder.AddUserMessage(ofstedSummaryPromptRetriever.GetPrompt());
            }
        }
        SetAISearchDataSourceForOfsted(briefing, chatCompletionOptions, azureSettings);
        SetConcernsPrompts(briefing);
        SetsBriefingResponseTemplate(briefing);

        promptBuilder.AddUserMessage(@$"Create a briefing for {briefing.AcademyName}");

        SetsAdditionalPrompt(briefing);

        return await CreateCompleteChatResponseAsync(chatClient, chatCompletionOptions);
    }

    /// <summary>
    /// Sets concerns information if concerns option is checked.
    /// </summary>
    /// <param name="briefing"></param>
    private void SetConcernsPrompts(BriefingParameters briefing)
    {
        if (briefing.Concerns)
        {
            var concernsData = concernsInformationRetriever.GetTrustConcerns();

            promptBuilder.AddUserMessage(concernsPromptRetriever.GetPrompt());
            promptBuilder.AddUserMessage(
                @$"Here are concerns related to the trust for this academy in the last 3 years associated with {briefing.AcademyName}: {concernsData}");
        }
    }

    /// <summary>
    /// Sets briefing response if template is provided in docx file format.
    /// </summary>
    /// <param name="briefing"></param>
    private void SetsBriefingResponseTemplate(BriefingParameters briefing)
    {
        if (!string.IsNullOrWhiteSpace(briefing.UploadFileContents))
        {
            //Cheating - adding the raw ofsted to data to fill in some of this information 
            promptBuilder.AddUserMessage($"The template content was originally a DOCX file, but I’ve converted it to HTML. Please fill it in and provide the output in Markdown format without any code fences: {briefing.UploadFileContents}");
        }
    }
    /// <summary>
    /// Sets additional prompt if additional information is provided
    /// </summary>
    /// <param name="briefing"></param>
    private void SetsAdditionalPrompt(BriefingParameters briefing)
    {
        if (!string.IsNullOrWhiteSpace(briefing.AdditionalPrompt))
        {
            promptBuilder.AddUserMessage(briefing.AdditionalPrompt);
        }
    }

    /// <summary>
    /// Create complete chat response.
    /// </summary>
    /// <param name="chatClient">An Azure Open AI chat client.</param>
    /// <param name="chatCompletionOptions">An instance of ChatCompletionOptions.</param>
    /// <returns></returns>
    private async Task<AIResult> CreateCompleteChatResponseAsync(ChatClient chatClient, ChatCompletionOptions chatCompletionOptions)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        try
        {
            var TotalTokens = 0;
            // Create the chat completion request
            ChatCompletion completion = await chatClient.CompleteChatAsync(promptBuilder.GetMessages(), chatCompletionOptions);

            // Print the response
            if (completion != null)
            {
                var chatResult = new StringBuilder();
                foreach (var content in completion.Content)
                {
                    TotalTokens += completion.Usage.TotalTokenCount;
                    string html = Markdown.ToHtml(content.Text);

                    chatResult.Append(html);
                }

                return new AIResult(chatResult.ToString(), promptBuilder.GetPrompt(), TotalTokens);
            }

            return new AIResult(string.Empty, "No response received.", -1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred when calling AI model");
            return new AIResult("", $"An error occurred: {ex.Message}", -1);
        }
    }
}
