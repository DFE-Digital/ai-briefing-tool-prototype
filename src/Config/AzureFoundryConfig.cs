namespace BriefingTool.Config;

public class AzureFoundryConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string ProjectEndpoint { get; set; } = string.Empty;
    public string OpenAiEndpoint { get; set; } = string.Empty;
    public string DeploymentModel { get; set; } = string.Empty; 
    public string ApiVersion { get; set; } = string.Empty;
}
public class FauAPIConfig
{
    public string OpenAiEndpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentModel { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = string.Empty;
    public string ModelGatewayName { get; set; } = string.Empty;
}  
public class AzureSearchConfig
{
    public string ApiKey { get; set; } = string.Empty; 
    public string Endpoint { get; set; } = string.Empty; 
    public string EstablishmentIndexName { get; set; } = string.Empty;
    public string OfstedIndexName { get; set; } = string.Empty;

}
