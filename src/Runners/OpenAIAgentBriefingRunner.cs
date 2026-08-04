using Azure.AI.Projects;
using Azure.Search.Documents;
using BriefingTool.Config;
using BriefingTool.Enums;
using BriefingTool.Factories;
using BriefingTool.Models;
using BriefingTool.Retrievers.Interfaces;
using BriefingTool.Runners.Interfaces;
using BriefingTool.Services.Interfaces;
using Markdig;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BriefingTool.Runners;

public class OpenAIAgentBriefingRunner(ILogger<OpenAIAgentBriefingRunner> logger, IPromptRetrieverService promptRetrieverService,
    IConcernsInformationRetriever concernsInformationRetriever,
    AzureFoundryConfig azureFoundryConfig, IAzureSearchService azureSearchService, AzureSearchConfig azureSearchConfig,
    IAzureOpenAIService azureOpenAIService, IMcpClientFactory mcpClientFactory) : IOpenAIAgentBriefingRunner
{
    private const string OfstedIndexName = "ofstedindex";

    [Experimental("AOAI002")]
    public async Task<AIResult> GetBriefing(OpenAiBriefingParameters briefing)
    {
        if (string.IsNullOrEmpty(briefing.AcademyName))
            return new AIResult("", "Enter an academy name", -1);

        await using var mcpClient = await mcpClientFactory.CreateClientAsync();
        var mcpTools = await mcpClient.ListToolsAsync();

        logger.LogInformation("OpenAI endpoint: {Endpoint}", azureFoundryConfig.OpenAiEndpoint);

        string instruction = promptRetrieverService.GetSystemPrompt(SystemPromptType.BriefingTool);

        var azureOpenAIClient = azureOpenAIService.InitialiseOpenAIClient(azureFoundryConfig.ApiKey, azureFoundryConfig.OpenAiEndpoint);
        AIAgent agent = azureOpenAIClient
            .GetChatClient(azureFoundryConfig.DeploymentModel)
            .AsIChatClient()
            .AsAIAgent( 
                name: "OpenAIBriefingAgent",
                tools: [.. mcpTools.Cast<AITool>()],
                instructions: instruction);

        logger.LogInformation("Agent created via AIProjectClient.AsAIAgent()"); 
         
        string userMessage = await BuildUserMessageAsync(briefing);
         
        AgentSession session = await agent.CreateSessionAsync();
         
        try
        {
            AgentResponse response = await agent.RunAsync(userMessage, session);

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
    private async Task<string> BuildUserMessageAsync(OpenAiBriefingParameters briefing)
    {
        var sb = new StringBuilder();

        await AppendOfstedSectionAsync(briefing, sb);
        AppendConcernsSection(briefing, sb);
        AppendTemplateSection(briefing, sb);

        sb.AppendLine($"Create a briefing for {briefing.AcademyName}");

        if (!string.IsNullOrWhiteSpace(briefing.AdditionalPrompt))
            sb.AppendLine(briefing.AdditionalPrompt);

        return sb.ToString().Trim();
    }

    [Experimental("AOAI002")]
    private async Task AppendOfstedSectionAsync(OpenAiBriefingParameters briefing, StringBuilder sb)
    {
        if (!briefing.Ofsted && !briefing.OfstedSummary)
            return;

        if (briefing.Ofsted)
        {
            sb.AppendLine(promptRetrieverService.GetUserPrompt(UserPromptType.Ofsted));
            sb.AppendLine();
        }

        if (briefing.OfstedSummary)
        {
            sb.AppendLine(promptRetrieverService.GetUserPrompt(UserPromptType.OfstedSummary));
            sb.AppendLine();
        }

        SearchClient ofstedSearchClient = azureSearchService.CreateSearchClient(
            azureSearchConfig.OfstedIndexName);

        string? ofstedContext = await azureSearchService.GetContentAsync(
            ofstedSearchClient, briefing.AcademyName);

        if (!string.IsNullOrWhiteSpace(ofstedContext))
        {
            sb.AppendLine("Ofsted information retrieved:");
            sb.AppendLine(ofstedContext);
            sb.AppendLine();
            logger.LogInformation("Ofsted context retrieved via AI Search.");
        }
    }

    [Experimental("AOAI002")]
    private void AppendConcernsSection(OpenAiBriefingParameters briefing, StringBuilder sb)
    {
        if (!briefing.Concerns)
            return;

        var concernsData = concernsInformationRetriever.GetTrustConcerns();
        sb.AppendLine(promptRetrieverService.GetUserPrompt(UserPromptType.Concerns));
        sb.AppendLine(
            $"Here are concerns related to the trust for this academy in the last 3 years " +
            $"associated with {briefing.AcademyName}: {concernsData}");
        sb.AppendLine();
    }

    [Experimental("AOAI002")]
    private void AppendTemplateSection(OpenAiBriefingParameters briefing, StringBuilder sb)
    {
        if (string.IsNullOrWhiteSpace(briefing.UploadFileContents))
            return;

        sb.AppendLine(promptRetrieverService.GetUserPrompt(UserPromptType.Uploads));
        sb.AppendLine(
            "The template content was originally a DOCX file, but I've converted it to HTML. " +
            "Please fill it in and provide the output in Markdown format without any code fences:");
        sb.AppendLine(briefing.UploadFileContents);
        sb.AppendLine();
    } 
    private AIResult BuildResult(AgentResponse response, string instruction)
    {
        if (string.IsNullOrWhiteSpace(response.Text))
        {
            logger.LogWarning("Agent returned an empty response.");
            return new AIResult(string.Empty, "No response received.", -1);
        }

        string html = Markdown.ToHtml(response.Text);
        var totalTokens = response.Usage?.TotalTokenCount;
        return new AIResult(html, instruction, totalTokens.GetValueOrDefault());
    }
}