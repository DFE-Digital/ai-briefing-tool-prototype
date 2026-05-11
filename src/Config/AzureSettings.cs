namespace BriefingTool.Config;

public class AzureSettings
{
    public string AzureOpenaiKey { get; set; } = string.Empty;
    public string AzureSearchKey { get; set; } = string.Empty;
    public string AzureOpenaiEndpoint { get; set; } = string.Empty;
    public string AzureSearchEndpoint { get; set; } = string.Empty;
    public string AzureOpenaiDeployment { get; set; } = string.Empty;
    public string AzureEmbeddingDeployment { get; set; } = string.Empty;
    public string AzureSearchIndex { get; set; } = string.Empty;

}
