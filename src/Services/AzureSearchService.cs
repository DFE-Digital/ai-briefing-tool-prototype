using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using BriefingTool.Config;
using BriefingTool.Services.Interfaces;
using System.Text;

namespace BriefingTool.Services;

public class AzureSearchService(AzureSearchConfig azureSearchConfig) : IAzureSearchService
{
    public SearchClient CreateSearchClient(string indexName)
    {
        return new SearchClient(new Uri(azureSearchConfig.Endpoint), indexName, new AzureKeyCredential(azureSearchConfig.ApiKey));
    }

    public async Task<string> GetContentAsync(SearchClient searchClient, string query, int size = 5)
    {
        ArgumentNullException.ThrowIfNull(searchClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        SearchResults<SearchDocument> results = await searchClient.SearchAsync<SearchDocument>(query, new SearchOptions
        {
             Size = size
        });

        var context = new StringBuilder();

        await foreach (SearchResult<SearchDocument> result in results.GetResultsAsync())
        {
            var documentText = string.Join(" ", result.Document
                .Where(kv => kv.Value is string && !string.IsNullOrWhiteSpace(kv.Value?.ToString()))
                .Select(kv => kv.Value.ToString()));

            if (!string.IsNullOrWhiteSpace(documentText))
                context.AppendLine(documentText);
        }

        return context.ToString();
    }
}