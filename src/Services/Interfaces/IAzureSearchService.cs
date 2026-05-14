using Azure.Search.Documents;
using BriefingTool.Config;

namespace BriefingTool.Services.Interfaces
{
    public interface IAzureSearchService
    {
        SearchClient CreateSearchClient(AzureSettings azureSettings, string indexName);
        Task<string> GetContentAsync(SearchClient searchClient, string query);
    }
}
