using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using BriefingTool.Config;
using BriefingTool.Enums;
using BriefingTool.Models;
using BriefingTool.Retrievers.Interfaces;
using BriefingTool.Runners.Interfaces;
using BriefingTool.Services.Interfaces;
using OpenAI.Responses;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BriefingTool.Runners;

public class FoundryHostedAgentBriefingRunner(
    ILogger<FoundryHostedAgentBriefingRunner> logger,
    IPromptRetrieverService promptRetrieverService,
    IConcernsInformationRetriever concernsInformationRetriever,
    AzureSettings azureSettings,
    FoundryHostedAgentConfig foundryHostedAgentConfig) : IBriefingRunner
{
    [Experimental("AOAI001")]
    public async Task<AIResult> GetBriefing(BriefingParameters briefing)
    {
        if (string.IsNullOrEmpty(briefing.AcademyName))
            return new AIResult("", "Enter an academy name", -1); 
        logger.LogInformation("AI endpoint: {Endpoint}", azureSettings.AzureProjectEndpoint);

        try
        {
            AIProjectClient projectClient = new(
                endpoint: new Uri(azureSettings.AzureProjectEndpoint), 
                tokenProvider: new DefaultAzureCredential());

            AgentReference agentReference = new(
                name: foundryHostedAgentConfig.Name,
                version: foundryHostedAgentConfig.Version);

            ProjectResponsesClient responseClient = projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgent(agentReference);

            string userMessage = await BuildUserMessageAsync(briefing);

            ResponseResult response = await responseClient.CreateResponseAsync(userMessage);

            logger.LogInformation("Agent response received successfully.");

            return BuildResult(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Agent run failed.");
            return new AIResult("", $"Agent run failed: {ex.Message}", -1);
        }
    }

    private async Task<string> BuildUserMessageAsync(
        BriefingParameters briefing)
    {
        var sb = new StringBuilder(); 

        await AppendOfstedSectionAsync(briefing, sb);
        AppendConcernsSection(briefing, sb);
        AppendTemplateSection(briefing, sb);

        sb.AppendLine($"Create a briefing for {briefing.AcademyName}");

        if (!string.IsNullOrWhiteSpace(briefing.AdditionalPrompt))
            sb.AppendLine(briefing.AdditionalPrompt);

        return sb.ToString();
    }

    private void AppendTemplateSection(BriefingParameters briefing, StringBuilder sb)
    {
        if (!string.IsNullOrWhiteSpace(briefing.UploadFileContents))
        {
            sb.AppendLine(promptRetrieverService.GetUserPrompt(UserPromptType.Uploads));
            sb.AppendLine(
                $"The template content was originally a DOCX file, but I've converted it to HTML. " +
                $"Please fill it in and provide the output in Markdown format without any code fences: " +
                $"{briefing.UploadFileContents}\n");
        }
    }
    private void AppendConcernsSection(BriefingParameters briefing, StringBuilder sb)
    {
        if (!briefing.Concerns) return;

        var concernsData = concernsInformationRetriever.GetTrustConcerns();
        sb.AppendLine(promptRetrieverService.GetUserPrompt(UserPromptType.Concerns));
        sb.AppendLine(
            $"Here are concerns related to the trust for this academy in the last 3 years " +
            $"associated with {briefing.AcademyName}: {concernsData}\n");
    }

    private async Task AppendOfstedSectionAsync(BriefingParameters briefing, StringBuilder sb)
    {
        if (!briefing.Ofsted && !briefing.OfstedSummary) return;

        if (briefing.Ofsted)
            sb.AppendLine(promptRetrieverService.GetUserPrompt(UserPromptType.Ofsted));

        if (briefing.OfstedSummary)
            sb.AppendLine(promptRetrieverService.GetUserPrompt(UserPromptType.OfstedSummary)); 
    }
    [Experimental("AOAI001")]
    private AIResult BuildResult(ResponseResult response)
    {
        try
        {
            var briefingResponse = response.GetOutputText();
            if (string.IsNullOrWhiteSpace(briefingResponse))
            {
               briefingResponse = ExtractTextFromOutputItems(response);
            }
            return !string.IsNullOrWhiteSpace(briefingResponse)
                ? new AIResult(briefingResponse, string.Empty, response.Usage.TotalTokenCount)
                : new AIResult(string.Empty, "No response received.", -1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred when reading agent response.");
            return new AIResult("", $"An error occurred: {ex.Message}", -1);
        }
    }
    [Experimental("AOAI001")]
    private static string? ExtractTextFromOutputItems(ResponseResult response)
    {
        var sb = new StringBuilder();

        foreach (var item in response.OutputItems)
        { 
            // Text output from assistant message
            /*if (item is MessageResponseItem message && message.Role == MessageRole.Assistant)
            {
                foreach (var content in message.Content)
                {
                    if (content is ResponseContentPart part
                        && part.Kind == ResponseContentPartKind.OutputText)
                    {
                        sb.Append(part.Text);
                    }
                }
            }*/
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }
}