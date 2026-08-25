using Azure.Search.Documents;

namespace BriefingTool.Services.Interfaces
{
    public interface IAzureSearchService
    {
        SearchClient CreateSearchClient(string indexName);
        Task<string> GetContentAsync(SearchClient searchClient, string query, int size = 5);
    }
}
