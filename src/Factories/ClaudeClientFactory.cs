using Anthropic;
using Anthropic.Models.Messages;
using BriefingTool.Config;
using BriefingTool.Models;

namespace BriefingTool.Factories;

public interface IClaudeClientFactory
{
    AnthropicClient InitialiseAnthropicClient(); 
    Task<AnthropicMessageResponse> PostMessageAsync(AnthropicClient anthropicClient, string systemPrompt, IEnumerable<MessageParam> messages, CancellationToken cancellationToken = default);
}
public class ClaudeClientFactory(ClaudeFoundryConfig claudeFoundryConfig, ILogger<ClaudeClientFactory> logger) : IClaudeClientFactory
{
    public AnthropicClient InitialiseAnthropicClient() => new()
    {
        ApiKey = claudeFoundryConfig.ApiKey,
        BaseUrl = claudeFoundryConfig.Endpoint
    }; 
    public async Task<AnthropicMessageResponse> PostMessageAsync(AnthropicClient anthropicClient, string systemPrompt, IEnumerable<MessageParam> messages, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new MessageCreateParams
            {
                Thinking = new ThinkingConfigAdaptive(),
                OutputConfig = new OutputConfig { Effort = "medium" },
                System = systemPrompt,
                MaxTokens = ClaudeFoundryConfig.EnsureMinTokens(claudeFoundryConfig.ThinkingEffort, claudeFoundryConfig.MaxTokens),
                Model = claudeFoundryConfig.DeploymentModel,
                Messages = [.. messages]
            };

             
            var message = await anthropicClient.Messages.Create(request, cancellationToken);
            var response = new AnthropicMessageResponse
            {
                TotalTokens = message.Usage.InputTokens + message.Usage.OutputTokens,
                Content = string.Concat(message.Content
                    .Select(block => block.TryPickText(out var textBlock) ? textBlock.Text : null)
                    .Where(text => text is not null))
            };

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while posting message to Claude API");
            throw;
        }
    }
} 
