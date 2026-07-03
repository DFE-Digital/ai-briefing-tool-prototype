using BriefingTool.Builders.Interfaces;
using BriefingTool.Config;
using BriefingTool.Enums;
using BriefingTool.Factories;
using BriefingTool.Models;
using BriefingTool.Retrievers.Interfaces;
using BriefingTool.Runners.Interfaces;
using BriefingTool.Services.Interfaces;
using Markdig;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using OpenAI.Chat;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace BriefingTool.Runners;

public class McpBriefingRunner(ILogger<McpBriefingRunner> logger,
    IPromptRetrieverService promptRetrieverService,
    IConcernsInformationRetriever concernsInformationRetriever,
    AzureSettings azureSettings,
    IAzureOpenAIService azureOpenAIService,
    IPromptBuilder promptBuilder,
    IMcpClientFactory mcpClientFactory) : IBriefingRunner
{
    [Experimental("AOAI001")]
    public async Task<AIResult> GetBriefing(BriefingParameters briefing)
    {
        if (string.IsNullOrEmpty(briefing.AcademyName))
        {
            return new AIResult("", "Enter an academy name", -1);
        }
        logger.LogInformation("AI endpoint: {Endpoint}", azureSettings.AzureOpenaiEndpoint);
        
        var chatClient = azureOpenAIService.GetChatClient(azureSettings.AzureOpenaiKey, azureSettings.AzureOpenaiEndpoint, azureSettings.AzureOpenaiDeployment);
        await using var mcpClient = await mcpClientFactory.CreateClientAsync();
        var systemMessage = await mcpClientFactory.GetPromptAsync(mcpClient, "GetSystemPrompt", "BriefingTool");
        promptBuilder.AddSystemMessage(systemMessage!);

        var chatCompletionOptions = azureOpenAIService.CreateChatCompletionOptions(); 
         
        void SetAISearchDataSourceForOfsted(BriefingParameters briefing)
        { 
            if (briefing.Ofsted)
            {
                promptBuilder.AddUserMessage(promptRetrieverService.GetUserPrompt(UserPromptType.Ofsted)); 
            }
            if (briefing.OfstedSummary)
            {
                promptBuilder.AddUserMessage(promptRetrieverService.GetUserPrompt(UserPromptType.OfstedSummary));
            } 
        }
        SetAISearchDataSourceForOfsted(briefing);
        SetConcernsPrompts(briefing);
        SetsBriefingResponseTemplate(briefing);

        promptBuilder.AddUserMessage(@$"Create a briefing for {briefing.AcademyName}");

        SetsAdditionalPrompt(briefing);

        return await CreateCompleteChatResponseAsync(chatClient, mcpClient, chatCompletionOptions);
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
    private async Task<AIResult> CreateCompleteChatResponseAsync(ChatClient chatClient, McpClient mcpClient, ChatCompletionOptions chatCompletionOptions)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        try
        {
            var totalTokens = 0;  

            var tools = await mcpClient.ListToolsAsync();
            foreach (var tool in tools)
            {
                chatCompletionOptions.Tools.Add(mcpClientFactory.ConvertToChatTool(tool));
            }
            
            var messages = promptBuilder.GetMessages().ToList();
            ChatCompletion completion;

            do
            {
                completion = await chatClient.CompleteChatAsync(messages, chatCompletionOptions);

                if (completion.FinishReason == ChatFinishReason.ToolCalls)
                {
                    // Add assistant's tool-call turn to history
                    messages.Add(new AssistantChatMessage(completion));

                    // Execute each tool via MCP and collect results
                    foreach (var toolCall in completion.ToolCalls)
                    {
                        string toolResult;
                        try
                        {
                            var mcpResult = await mcpClient.CallToolAsync(
                                toolCall.FunctionName,
                                JsonSerializer.Deserialize<Dictionary<string, object?>>(
                                    toolCall.FunctionArguments)
                                ?? []
                            );

                            var rawText = string.Join("\n", mcpResult.Content
                                .Where(c => c.Type == "text")
                                .Select(c => c.ToAIContent()));

                            // Filter by score > 7 if the tool result is JSON
                            toolResult = ProcessResults(rawText, 7, limit: 10);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Tool call '{Tool}' failed", toolCall.FunctionName);
                            toolResult = $"Error calling tool: {ex.Message}";
                        }

                        messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                    }
                }

            } while (completion.FinishReason == ChatFinishReason.ToolCalls);

            // Model has finished — read the final content response
            totalTokens += completion.Usage?.TotalTokenCount ?? 0;

            var chatResult = new StringBuilder();
            foreach (var content in completion.Content)
            {
                chatResult.Append(Markdown.ToHtml(content.Text));
            }

            return chatResult.Length > 0
                ? new AIResult(chatResult.ToString(), promptBuilder.GetPrompt(), totalTokens)
                : new AIResult(string.Empty, "No response received.", -1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred when calling AI model");
            return new AIResult(string.Empty, $"An error occurred: {ex.Message}", -1);
        }
    }
    private static string ProcessResults(string rawJson, double minScore, int limit)
    {
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;
        var totalCount = root.GetProperty("totalCount").GetInt32();
        var resultElement = root.GetProperty("results");

        var result = resultElement.ValueKind switch
        {
            JsonValueKind.Array => BuildArrayResponse(resultElement, totalCount, minScore, limit),
            _ => BuildPassthroughResponse(resultElement, totalCount)
        };

        return JsonSerializer.Serialize(result);
    }

    private static object BuildArrayResponse(JsonElement resultArray, int totalCount, double minScore, int limit)
    {
        var filtered = resultArray
            .EnumerateArray()
            .Where(item => PassesScoreFilter(item, minScore))
            .Take(limit)
            .ToList();

        return new
        {
            TotalCount = totalCount,
            Showing = filtered.Count,
            HasMore = totalCount > limit,
            Results = filtered
        };
    }

    private static object BuildPassthroughResponse(JsonElement resultElement, int totalCount)
    {
        return new
        {
            TotalCount = totalCount,
            Showing = 1,
            HasMore = false,
            Results = resultElement
        };
    }

    private static bool PassesScoreFilter(JsonElement item, double minScore)
    {
        if (item.ValueKind != JsonValueKind.Object|| !item.TryGetProperty("Score", out var score))
            return true;

        if (!score.TryGetDouble(out var value))
            return false;

        return value > minScore;
    }
}
