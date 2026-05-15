using ModelContextProtocol.Client;

namespace BriefingTool.Config;

public sealed class McpClientConfig 
{
    public string Endpoint { get; set; } = string.Empty;
    public string Name { get; set; } = "MCP Client";
    public string Version { get; set; } = "1.0.0";
    public string ProtocolVersion { get; set; } = "2025-11-25";
    public HttpTransportMode TransportMode { get; set; } = HttpTransportMode.StreamableHttp;
}
public class AzureAdConfig
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
}
