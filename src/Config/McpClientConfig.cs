using ModelContextProtocol.Client;

namespace BriefingTool.Config;

public sealed class McpClientConfig
{
    public string Endpoint { get; set; } = string.Empty; 
    public string FoundryIqMcpEndpoint { get; set; } = string.Empty;    
    public string Name { get; set; } = "MCP Client";
    public string Version { get; set; } = "1.0.0";
    public string ProtocolVersion { get; set; } = "2025-11-25";
    public string ApiKey { get; set; } = string.Empty;
    public HttpTransportMode TransportMode { get; set; } = HttpTransportMode.StreamableHttp;
}
public sealed class ClaudeFoundryConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentModel { get; set; } = string.Empty;   
    public string ApiVersion { get; set; } = "2023-06-01";
    public int MaxTokens { get; set; } = 4096;
    public string ThinkingEffort { get; set; } = "medium";

    private static readonly Dictionary<string, int> MinTokensByEffort = new()
    {
        { "low",    2048 },
        { "medium", 4096 },
        { "high",   8192 },
        { "xhigh",  16000 },
        { "max",    32000 },
    };
    public static int EnsureMinTokens(string effort, int requestedMaxTokens)
    {
        if (MinTokensByEffort.TryGetValue(effort, out var minRequired) && requestedMaxTokens < minRequired)
        {
            return minRequired; // silently raise to the safe floor
        }
        return requestedMaxTokens;
    }
}
public class AzureAdConfig
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
}
