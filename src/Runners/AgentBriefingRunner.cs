using Azure.AI.Projects;
using Azure.Identity;
using Azure.Search.Documents;
using BriefingTool.Config;
using BriefingTool.Enums;
using BriefingTool.Models;
using BriefingTool.Retrievers.Interfaces;
using BriefingTool.Runners.Interfaces;
using BriefingTool.Services.Interfaces;
using Markdig;
using Microsoft.Agents.AI;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BriefingTool.Runners;

public class AgentBriefingRunner(ILogger<AgentBriefingRunner> logger, IPromptRetrieverService promptRetrieverService,
    IConcernsInformationRetriever concernsInformationRetriever,
    AzureSettings azureSettings, IAzureSearchService azureSearchService) : IBriefingRunner
{
    private const string OfstedIndexName = "ofstedindex";

    [Experimental("AOAI002")]
    public async Task<AIResult> GetBriefing(BriefingParameters briefing)
    {
        if (string.IsNullOrEmpty(briefing.AcademyName))
            return new AIResult("", "Enter an academy name", -1);

        logger.LogInformation("AI endpoint: {Endpoint}", azureSettings.AzureOpenaiEndpoint);

        string instruction = promptRetrieverService.GetSystemPrompt(SystemPromptType.BriefingTool);

        AIAgent agent = new AIProjectClient(
                new Uri(azureSettings.AzureProjectEndpoint),
                new DefaultAzureCredential())
            .AsAIAgent(
                model: azureSettings.AzureOpenaiDeployment,
                name: "BriefingAgent",
                instructions: instruction);

        logger.LogInformation("Agent created via AIProjectClient.AsAIAgent()");
         
        SearchClient establishmentSearchClient = azureSearchService.CreateSearchClient(
            azureSettings,
            azureSettings.AzureSearchIndex);

        string? establishmentContext = await azureSearchService.GetContentAsync(
            establishmentSearchClient,
            briefing.AcademyName);

        logger.LogInformation("Establishment context retrieved via AI Search.");
         
        string userMessage = await BuildUserMessageAsync(briefing, establishmentContext);
         
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
    private async Task<string> BuildUserMessageAsync(BriefingParameters briefing, string? establishmentContext)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(establishmentContext))
        {
            sb.AppendLine("Establishment, school or academy information retrieved:");
            sb.AppendLine(establishmentContext);
            sb.AppendLine();
        }

        await AppendOfstedSectionAsync(briefing, sb);
        AppendConcernsSection(briefing, sb);
        AppendTemplateSection(briefing, sb);

        sb.AppendLine($"Create a briefing for {briefing.AcademyName}");

        if (!string.IsNullOrWhiteSpace(briefing.AdditionalPrompt))
            sb.AppendLine(briefing.AdditionalPrompt);

        return sb.ToString().Trim();
    }

    [Experimental("AOAI002")]
    private async Task AppendOfstedSectionAsync(BriefingParameters briefing, StringBuilder sb)
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
            azureSettings, OfstedIndexName);

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
    private void AppendConcernsSection(BriefingParameters briefing, StringBuilder sb)
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
    private static void AppendTemplateSection(BriefingParameters briefing, StringBuilder sb)
    {
        if (string.IsNullOrWhiteSpace(briefing.UploadFileContents))
            return;

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