using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using BriefingTool.Config;
using BriefingTool.Pages;
using Markdig;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace BriefingTool.Services
{

    public record AIResult(string output, string debug, int TotalTokens);

    public record BriefingParameters(string AcademyName, bool Ofsted, bool Concerns, bool Financial, string? AdditionalPrompt, string? UploadFileContents);


    public interface IBriefingRunner
    {
        Task<AIResult> GetBriefing(BriefingParameters briefing);
    }

    public class BriefingRunner(ILogger<IndexModel> logger,
        IConfiguration configuration,
        IBasePromptRetriever basePromptRetriever,
        IConcernsPromptRetriever concernsPromptRetriever,
        IAcademyInformationRetriever academyInformationRetriever,
        IOfstedPromptRetriever ofstedPromptRetriever,
        IOfstedSummaryPromptRetriever ofstedSummaryPromptRetriever,
        IConcernsInformationRetriever concernsInformationRetriever,
        IOptions<AzureSettings> settings) : IBriefingRunner
    {


        private const string OfstedIndexName = "ofstedindex";

        [Experimental("AOAI001")]
        public async Task<AIResult> GetBriefing(BriefingParameters briefing)
        {
            var azureSettings = settings.Value;

            if (string.IsNullOrEmpty(briefing.AcademyName))
            {

                return new AIResult("", "Enter an academy name", -1);
            }

            // Create chat completion options

            var options = new ChatCompletionOptions
            {
                Temperature = (float)0.7,
                MaxOutputTokenCount = 6553,

                TopP = (float)0.95,
                FrequencyPenalty = (float)0,
                PresencePenalty = (float)0
            };

            logger.LogInformation($"AI endpoint: {azureSettings.AzureOpenaiEndpoint}");

            AzureKeyCredential credential = new AzureKeyCredential(azureSettings.AzureOpenaiKey);

            // Initialize the AzureOpenAIClient
            AzureOpenAIClient azureClient = new(new Uri(azureSettings.AzureOpenaiEndpoint), credential);

            // Initialize the ChatClient with the specified deployment name
            ChatClient chatClient = azureClient.GetChatClient("UC021-gpt-4o");

            var academyData = academyInformationRetriever.GetAcademyInformation(briefing.AcademyName);


            var jsonAcademyData = JsonSerializer.Serialize(academyData);

            var promptBuilder = new PromptBuilder();

            promptBuilder.AddSystemMessage(basePromptRetriever.GetPrompt());

            // AI Search data source
            if (briefing.Ofsted)
            {
                options.AddDataSource(new AzureSearchChatDataSource()
                {
                    Endpoint = new Uri(settings.Value.AzureSearchEndpoint),
                    IndexName = OfstedIndexName,
                    Authentication = DataSourceAuthentication.FromApiKey(settings.Value.AzureSearchKey)
                });

                promptBuilder.AddSystemMessage(ofstedPromptRetriever.GetPrompt());
                promptBuilder.AddSystemMessage(ofstedSummaryPromptRetriever.GetPrompt());
            }

            //Data source provided by API
            if (briefing.Concerns)
            {
                var concernsData = concernsInformationRetriever.GetTrustConcerns();

                promptBuilder.AddSystemMessage(concernsPromptRetriever.GetPrompt());
                promptBuilder.AddSystemMessage(
                    @$"Here are concerns related to the trust for this academy in the last 3 years associated with {briefing.AcademyName}: {concernsData}");
            }

            if (!string.IsNullOrWhiteSpace(briefing.UploadFileContents))
            {
                //Cheating - adding the raw ofsted to data to fill in some of this information
                promptBuilder.AddSystemMessage(@$"Here is ofsted inspection data associated with {briefing.AcademyName} in JSON format: {jsonAcademyData}");
                promptBuilder.AddSystemMessage($"Here is the contents of the template which was originally docx file but I have converted html format that needs to be filled: {briefing.UploadFileContents}");
            }

            promptBuilder.AddUserMessage(@$"Create a briefing for {briefing.AcademyName}");

            if (!string.IsNullOrWhiteSpace(briefing.AdditionalPrompt))
            {
                promptBuilder.AddUserMessage(briefing.AdditionalPrompt);
            }

            try
            {
                var TotalTokens = 0;
                // Create the chat completion request
                ChatCompletion completion = await chatClient.CompleteChatAsync(promptBuilder.GetMessages(), options);

                var chatResult = new StringBuilder();

                // Print the response
                if (completion != null)
                {
                    foreach (var content in completion.Content)
                    {
                        TotalTokens += completion.Usage.TotalTokenCount;
                        string html = Markdown.ToHtml(content.Text);

                        chatResult.Append(html);
                    }

                    return new AIResult(chatResult.ToString(), promptBuilder.GetPrompt(), TotalTokens);
                }

                return new AIResult("", "No response received.", -1);
            }
            catch (Exception ex)
            {
                return new AIResult("", $"An error occurred: {ex.Message}", -1);
            }
        }

    }
}
