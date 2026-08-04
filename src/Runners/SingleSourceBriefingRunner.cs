using Azure.AI.OpenAI.Chat;
using BriefingTool.Builders.Interfaces;
using BriefingTool.Config;
using BriefingTool.Enums;
using BriefingTool.Models;
using BriefingTool.Retrievers.Interfaces;
using BriefingTool.Runners.Interfaces;
using BriefingTool.Services.Interfaces;
using Markdig;
using OpenAI.Chat;
using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BriefingTool.Runners;

public class SingleSourceBriefingRunner(ILogger<SingleSourceBriefingRunner> logger,
    IPromptRetrieverService promptRetrieverService,
    IConcernsInformationRetriever concernsInformationRetriever,
    FauAPIConfig fauAPIConfig,
    AzureSearchConfig azureSearchConfig,
    IAzureOpenAIService azureOpenAIService,
    IPromptBuilder promptBuilder) : IBriefingRunner
{
    [Experimental("AOAI001")]
    public async Task<AIResult> GetBriefing(BriefingParameters briefing)
    {
        if (string.IsNullOrEmpty(briefing.AcademyName))
        {
            return new AIResult("", "Enter an academy name", -1);
        }
        logger.LogInformation("AI endpoint: {Endpoint}", fauAPIConfig.OpenAiEndpoint);

        var chatClient = azureOpenAIService.GetChatClient(fauAPIConfig.ApiKey, fauAPIConfig.OpenAiEndpoint, fauAPIConfig.DeploymentModel); 
        promptBuilder.AddSystemMessage(promptRetrieverService.GetSystemPrompt(SystemPromptType.BriefingTool));

        var chatCompletionOptions = azureOpenAIService.CreateChatCompletionOptions();


        // AI Search data source
        AzureSearchChatDataSource GetChatDataSource(AzureSearchConfig azureSearchConfig)
        {
            return new AzureSearchChatDataSource()
            {
                Endpoint = new Uri(azureSearchConfig.Endpoint),
                IndexName = azureSearchConfig.OfstedIndexName,
                Authentication = DataSourceAuthentication.FromApiKey(azureSearchConfig.ApiKey),
                MaxSearchQueries = 1
            };
        }
        void SetAISearchDataSourceForOfsted(BriefingParameters briefing, ChatCompletionOptions chatCompletionOptions, FauAPIConfig fauAPIConfig)
        {
            // AI Search data source
            if (briefing.Ofsted)
            {
                chatCompletionOptions.AddDataSource(GetChatDataSource(azureSearchConfig));

                promptBuilder.AddUserMessage(promptRetrieverService.GetUserPrompt(UserPromptType.Ofsted));
            }
            if (briefing.OfstedSummary)
            {
                if (!briefing.Ofsted)
                    chatCompletionOptions.AddDataSource(GetChatDataSource(azureSearchConfig));

                promptBuilder.AddUserMessage(promptRetrieverService.GetUserPrompt(UserPromptType.OfstedSummary));
            }
        }
        SetAISearchDataSourceForOfsted(briefing, chatCompletionOptions, fauAPIConfig);
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

            promptBuilder.AddUserMessage(promptRetrieverService.GetUserPrompt(UserPromptType.Concerns));
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
            promptBuilder.AddUserMessage(promptRetrieverService.GetUserPrompt(UserPromptType.Uploads));
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
        catch (ClientResultException ex)
        {
            logger.LogError(ex, "An error occurred when calling AI model");
            return new AIResult("", $"An error occurred: {ex.Message}", -1);
        }
    }
}
