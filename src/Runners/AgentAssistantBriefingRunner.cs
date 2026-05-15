using Azure.Search.Documents;
using BriefingTool.Config;
using BriefingTool.Models;
using BriefingTool.Retrievers.Interfaces;
using BriefingTool.Runners.Interfaces;
using BriefingTool.Services.Interfaces;
using Markdig;
using OpenAI.Assistants;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BriefingTool.Runners;

public class AgentAssistantBriefingRunner(
    ILogger<AgentAssistantBriefingRunner> logger,
    IBasePromptRetriever basePromptRetriever,
    IConcernsPromptRetriever concernsPromptRetriever,
    IOfstedPromptRetriever ofstedPromptRetriever,
    IOfstedSummaryPromptRetriever ofstedSummaryPromptRetriever,
    IConcernsInformationRetriever concernsInformationRetriever,
    AzureSettings azureSettings,
    IAzureOpenAIService azureOpenAIService,
    IAzureSearchService azureSearchService) : IBriefingRunner
{
    private const string OfstedIndexName = "ofstedindex";

    [Experimental("AOAI002")]
    public async Task<AIResult> GetBriefing(BriefingParameters briefing)
    {
        if (string.IsNullOrEmpty(briefing.AcademyName))
            return new AIResult("", "Enter an academy name", -1);

        logger.LogInformation("AI endpoint: {Endpoint}", azureSettings.AzureOpenaiEndpoint);

        var openAIClient = azureOpenAIService.InitialiseOpenAIClient(azureSettings.AzureOpenaiKey, azureSettings.AzureOpenaiEndpoint);

        var assistantClient = openAIClient.GetAssistantClient();
         
        var instruction = basePromptRetriever.GetPrompt();
         
        SearchClient establishmentClient = azureSearchService.CreateSearchClient(azureSettings, azureSettings.AzureSearchIndex); 
        var establishmentContext = await azureSearchService.GetContentAsync(establishmentClient, briefing.AcademyName);
        logger.LogInformation("Establishment context retrieved via AI Search.");
          
         
        var agent = await azureOpenAIService.CreateAgentAssistantAsync( openAIClient, azureSettings.AzureOpenaiDeployment, instruction);

        try
        {
            AssistantThread thread = await assistantClient.CreateThreadAsync();

            try
            {
                var userMessageParts = await BuildUserMessagePartsAsync(briefing, establishmentContext);
                 
                await assistantClient.CreateMessageAsync(thread.Id, MessageRole.User, userMessageParts);
                 
                ThreadRun threadRun = await assistantClient.CreateRunAsync(thread.Id, agent.Id);
                 
                threadRun = await PollUntilCompleteAsync(assistantClient, thread.Id, threadRun);

                if (threadRun.Status == RunStatus.Failed || threadRun.Status == RunStatus.Cancelled)
                {
                    var reason = threadRun.LastError?.Message ?? threadRun.Status.ToString();
                    logger.LogError("Agent run did not complete. Status: {Status}, Reason: {Reason}", threadRun.Status, reason);
                    return new AIResult("", $"Agent run failed: {reason}", -1);
                }
                 
                return await BuildResultFromThreadAsync(assistantClient, thread.Id, threadRun, instruction);
            }
            finally
            { 
                await assistantClient.DeleteThreadAsync(thread.Id);
            }
        }
        finally
        { 
            await assistantClient.DeleteAssistantAsync(agent.Id);
        }
    }


    /// <summary>
    /// Builds user message parts for the agent thread,
    /// mirroring the existing promptBuilder flow.
    /// </summary>
    [Experimental("AOAI002")]
    private async Task<List<MessageContent>> BuildUserMessagePartsAsync(BriefingParameters briefing, string? establishmentContext)
    {
        var parts = new List<MessageContent>();

        if (!string.IsNullOrWhiteSpace(establishmentContext))
            parts.Add(MessageContent.FromText($"Establishment, school or academy information retrieved:\n{establishmentContext}"));

        await AddOfstedPartsAsync(briefing, parts);
        AddConcernsParts(briefing, parts); 
        AddTemplatePart(briefing, parts);

        // Main request
        parts.Add(MessageContent.FromText($"Create a briefing for {briefing.AcademyName}"));

        // Additional prompt — mirrors SetsAdditionalPrompt
        if (!string.IsNullOrWhiteSpace(briefing.AdditionalPrompt))
            parts.Add(MessageContent.FromText(briefing.AdditionalPrompt));

        return parts;
    }

    [Experimental("AOAI002")]
    private static void AddTemplatePart(BriefingParameters briefing, List<MessageContent> parts)
    {
        if (!string.IsNullOrWhiteSpace(briefing.UploadFileContents))
            parts.Add(MessageContent.FromText(
                $"The template content was originally a DOCX file, but I've converted it to HTML. " +
                $"Please fill it in and provide the output in Markdown format without any code fences: {briefing.UploadFileContents}"));
    }

    [Experimental("AOAI002")]
    private void AddConcernsParts(BriefingParameters briefing, List<MessageContent> parts)
    {
        if (briefing.Concerns)
        {
            var concernsData = concernsInformationRetriever.GetTrustConcerns();
            parts.Add(MessageContent.FromText(concernsPromptRetriever.GetPrompt()));
            parts.Add(MessageContent.FromText($"Here are concerns related to the trust for this academy in the last 3 years associated with {briefing.AcademyName}: {concernsData}"));
        }
    }

    [Experimental("AOAI002")]
    private async Task AddOfstedPartsAsync(BriefingParameters briefing, List<MessageContent> parts)
    {
        if (!briefing.Ofsted && !briefing.OfstedSummary)
            return;

        if (briefing.Ofsted)
            parts.Add(MessageContent.FromText(ofstedPromptRetriever.GetPrompt()));

        if (briefing.OfstedSummary)
            parts.Add(MessageContent.FromText(ofstedSummaryPromptRetriever.GetPrompt()));

        var searchClient = azureSearchService.CreateSearchClient(azureSettings, OfstedIndexName);
        var ofstedContext = await azureSearchService.GetContentAsync(searchClient, briefing.AcademyName);

        if (!string.IsNullOrWhiteSpace(ofstedContext))
        {
            parts.Add(MessageContent.FromText($"Ofsted information retrieved:\n{ofstedContext}"));
            logger.LogInformation("Ofsted context retrieved via AI Search.");
        }
    }

    /// <summary>
    /// Polls the run until it reaches a terminal status.
    /// </summary>
    [Experimental("AOAI002")]
    private async Task<ThreadRun> PollUntilCompleteAsync(AssistantClient assistantClient, string threadId, ThreadRun run, int pollIntervalMs = 1500, int maxAttempts = 60)
    {
        var attempts = 0;

        while (run.Status != RunStatus.Completed &&
               run.Status != RunStatus.Failed &&
               run.Status != RunStatus.Cancelled &&
               run.Status != RunStatus.Expired)
        {
            if (attempts++ >= maxAttempts)
            {
                logger.LogWarning("Agent run polling timed out after {Attempts} attempts.", attempts);
                break;
            }

            await Task.Delay(pollIntervalMs);
            run = await assistantClient.GetRunAsync(threadId, run.Id);
            logger.LogDebug("Run status: {Status}", run.Status);
        }

        return run;
    }

    /// <summary>
    /// Reads assistant messages from the thread and builds AIResult.
    /// </summary>
    [Experimental("AOAI002")]
    private async Task<AIResult> BuildResultFromThreadAsync(AssistantClient assistantClient, string threadId, ThreadRun run,  string instruction)
    {
        try
        {
            var chatResult = new StringBuilder();
            var totalTokens = run.Usage?.TotalTokenCount ?? 0;

            await foreach (ThreadMessage message in assistantClient.GetMessagesAsync(threadId))
            {
                if (message.Role == MessageRole.Assistant)
                {
                    foreach (var content in message.Content)
                    {
                        string html = Markdown.ToHtml(content.Text);
                        chatResult.Append(html);
                    }
                    break; // Only take the latest assistant message
                }
            }

            return chatResult.Length > 0
                ? new AIResult(chatResult.ToString(), instruction, totalTokens)
                : new AIResult(string.Empty, "No response received.", -1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred when reading agent response");
            return new AIResult("", $"An error occurred: {ex.Message}", -1);
        }
    }
}