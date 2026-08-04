using Anthropic.Models.Messages;
using BriefingTool.Builders.Interfaces;
using BriefingTool.Config;
using BriefingTool.Enums;
using BriefingTool.Factories;
using BriefingTool.Models;
using BriefingTool.Retrievers.Interfaces;
using BriefingTool.Runners.Interfaces;
using BriefingTool.Services.Interfaces;
using Markdig;
using System.Diagnostics.CodeAnalysis;

namespace BriefingTool.Runners;

public class ClaudeFoundryBriefingRunner(ILogger<ClaudeFoundryBriefingRunner> logger,
    IPromptRetrieverService promptRetrieverService,
    IConcernsInformationRetriever concernsInformationRetriever,   
    AzureSearchConfig azureSearchConfig,
    IPromptBuilder promptBuilder,
    IAzureSearchService azureSearchService,
    IClaudeClientFactory claudeClientFactory) : IBriefingRunner
{

    [Experimental("AOAI002")]
    public async Task<AIResult> GetBriefing(BriefingParameters briefing)
    { 
        if (string.IsNullOrEmpty(briefing.AcademyName))
            return new AIResult("", "Enter an academy name", -1);

        try
        {
            var instruction = promptRetrieverService.GetSystemPrompt(SystemPromptType.BriefingTool);
            var chatMessages = await BuildChatMessageAsync(briefing);
            
            using var anthropicClient = claudeClientFactory.InitialiseAnthropicClient();
            logger.LogInformation("Anthropic Client Created");
             
            var response = await claudeClientFactory.PostMessageAsync(anthropicClient, instruction, chatMessages, default); 

            logger.LogInformation("Agent run completed successfully.");

            return BuildResult(response, instruction);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Agent run failed.");
            return new AIResult("", $"Agent run failed: {ex.Message}", -1);
        }
        finally
        {
            //await session.DisposeAsyn();
            logger.LogInformation("Agent session disposed.");
        }
    }

    [Experimental("AOAI002")]
    private async Task<IEnumerable<MessageParam>> BuildChatMessageAsync(BriefingParameters briefing)
    { 
        await AppendOfstedSectionAsync(briefing);
        AppendConcernsSection(briefing);
        AppendTemplateSection(briefing);

        return promptBuilder.GetAnthropicMessages();
    }

    [Experimental("AOAI002")]
    private async Task AppendOfstedSectionAsync(BriefingParameters briefing)
    {
        if (!briefing.Ofsted && !briefing.OfstedSummary)
            return;
         
        if (briefing.Ofsted)
        {
            promptBuilder.AddAnthropicUserMessage(promptRetrieverService.GetUserPrompt(UserPromptType.Ofsted));
        }

        if (briefing.OfstedSummary)
        { 
            promptBuilder.AddAnthropicUserMessage(promptRetrieverService.GetUserPrompt(UserPromptType.OfstedSummary)); 
        }
        if (briefing.Ofsted || briefing.OfstedSummary)
        {
            var ofstedSearchClient = azureSearchService.CreateSearchClient(azureSearchConfig.OfstedIndexName);

            var ofstedContext = await azureSearchService.GetContentAsync(ofstedSearchClient, briefing.AcademyName);

            if (!string.IsNullOrWhiteSpace(ofstedContext))
            {
                promptBuilder.AddAnthropicUserMessage($"Here are Ofsted related to the trust for this academy associated with {briefing.AcademyName}: {ofstedContext}");
                promptBuilder.AddUserMessage(ofstedContext);
                logger.LogInformation("Ofsted context retrieved via AI Search.");
            }
        }
    }

    [Experimental("AOAI002")]
    private void AppendConcernsSection(BriefingParameters briefing)
    {
        if (!briefing.Concerns)
            return;

        var concernsData = concernsInformationRetriever.GetTrustConcerns();
        promptBuilder.AddAnthropicUserMessage(promptRetrieverService.GetUserPrompt(UserPromptType.Concerns));
        promptBuilder.AddAnthropicUserMessage(
            $"Here are concerns related to the trust for this academy in the last 3 years " +
            $"associated with {briefing.AcademyName}: {concernsData}");
        logger.LogInformation("Concerns context retrieved.");
    }

    [Experimental("AOAI002")]
    private void AppendTemplateSection(BriefingParameters briefing)
    {
        if (string.IsNullOrWhiteSpace(briefing.UploadFileContents))
            return;

        promptBuilder.AddAnthropicUserMessage(promptRetrieverService.GetUserPrompt(UserPromptType.Uploads));
        promptBuilder.AddAnthropicUserMessage(
            "The template content was originally a DOCX file, but I've converted it to HTML. " +
            "Please fill it in and provide the output in Markdown format without any code fences:");
        promptBuilder.AddAnthropicUserMessage(briefing.UploadFileContents);
        logger.LogInformation("Template context retrieved.");
    }
    private AIResult BuildResult(AnthropicMessageResponse response, string instruction)
    {
        if (string.IsNullOrWhiteSpace(response.Content))
        {
            logger.LogWarning("Agent returned an empty response.");
            return new AIResult(string.Empty, "No response received.", -1);
        }

        string html = Markdown.ToHtml(response.Content); 
        return new AIResult(html, instruction, response.TotalTokens);
    }
}