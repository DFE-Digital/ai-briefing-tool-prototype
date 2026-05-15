using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using BriefingTool.Config;
using BriefingTool.Services.Interfaces;
using System.Text;

namespace BriefingTool.Services;

public class AzureSearchService : IAzureSearchService
{
    public SearchClient CreateSearchClient(AzureSettings azureSettings, string indexName)
    {
        return new SearchClient(new Uri(azureSettings.AzureSearchEndpoint), indexName, new AzureKeyCredential(azureSettings.AzureSearchKey));
    }


    public async Task<string> GetContentAsync(SearchClient searchClient, string query)
    {
        ArgumentNullException.ThrowIfNull(searchClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        SearchResults<SearchDocument> results = await searchClient.SearchAsync<SearchDocument>(query);

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