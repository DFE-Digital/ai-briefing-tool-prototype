using BriefingTool.Config;
using BriefingTool.Services.Interfaces;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI.Chat;
using System.Net.Http.Headers;

namespace BriefingTool.Factories;

public interface IMcpClientFactory
{
    Task<McpClient> CreateClientAsync(bool IsApiKeyBasedAuthenticaation = false, CancellationToken cancellationToken = default);
    ChatTool ConvertToChatTool(McpClientTool mcpTool);
    Task<string?> GetPromptAsync(McpClient mcpClient, string promptName, string promptType);
}
public class McpClientFactory(McpClientConfig mcpClientConfig, ITokenService tokenService, ILogger<McpClientFactory> logger) : IMcpClientFactory
{
    private async Task<HttpClient> CreateHttpClientAsync(bool IsApiKeyBasedAuthenticaation = false)
    {
        var httpClient = new HttpClient();
        if (IsApiKeyBasedAuthenticaation)
        {
            httpClient.DefaultRequestHeaders.Remove("api-key");
            httpClient.DefaultRequestHeaders.Add("api-key", mcpClientConfig.ApiKey);
        }
        else
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await tokenService.GetAccessTokenAsync());

        return httpClient;
    }
    public async Task<McpClient> CreateClientAsync(bool IsApiKeyBasedAuthenticaation = false, CancellationToken cancellationToken = default)
    { 
        var httpClient = await CreateHttpClientAsync(IsApiKeyBasedAuthenticaation);

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(IsApiKeyBasedAuthenticaation? mcpClientConfig.FoundryIqMcpEndpoint :  mcpClientConfig.Endpoint),
            TransportMode = mcpClientConfig.TransportMode, 
        }, httpClient);

        var client = await McpClient.CreateAsync(
            transport,
            new McpClientOptions
            {
                ClientInfo = new Implementation
                {
                    Name = mcpClientConfig.Name,
                    Version = mcpClientConfig.Version
                },
                ProtocolVersion = mcpClientConfig.ProtocolVersion,
                Capabilities = new ClientCapabilities()
            },
            cancellationToken: cancellationToken);

        logger.LogInformation("MCP client created successfully");
        return client;
    } 

    public async Task<string?> GetPromptAsync(McpClient mcpClient, string promptName, string promptType)
    {
        try
        {
            var prompts = await mcpClient.ListPromptsAsync();

            if (!prompts.Any(p => p.Name == promptName))
                return null; 

            var promptResult = await mcpClient.GetPromptAsync(promptName, new Dictionary<string, object?>
            {
                { "promptType", promptType }
            });

            return promptResult.Messages
                .Where(m => m.Role == Role.Assistant || m.Role == Role.User)
                .Select(m => m.Content is TextContentBlock text ? text.Text : m.Content.ToString())
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get prompt '{PromptName}' from MCP server", promptName);
            return null;
        }
    }

    /// <summary>
    /// Build a JSON schema object for the function parameters. MCP tools carry their inputSchema as a JsonElement.
    /// </summary>
    /// <param name="mcpTool"></param>
    /// <returns></returns>
    public ChatTool ConvertToChatTool(McpClientTool mcpTool)
    {
        var schemaJson = mcpTool.JsonSchema.ToString(); 

        return ChatTool.CreateFunctionTool(
            functionName: mcpTool.Name,
            functionDescription: mcpTool.Description ?? mcpTool.Name,
            functionParameters: BinaryData.FromString(schemaJson));
    }
} 
