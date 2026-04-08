using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using BriefingTool.Config;
using BriefingTool.Indexers.Interfaces;
using BriefingTool.Services.Interfaces;
using OpenAI.Embeddings;
using System.Text.Json;

namespace BriefingTool.Indexers;
public class OfstedIndexer(AzureSettings azureSettings, IAzureOpenAIService azureOpenAIService) : IOfstedIndexer
{
    private const string OfstedIndexName = "ofstedindex";

    public async Task CreateIndex()
    {
        var indexClient = InitializeSearchIndexClient(azureSettings.AzureSearchKey, azureSettings.AzureSearchEndpoint);
        var searchClient = indexClient.GetSearchClient(OfstedIndexName);

        await SetupIndexAsync(indexClient);
        await UploadSampleDocumentsAsync(searchClient);
    }

    /// <summary>
    /// Initilise search index client.
    /// </summary>
    /// <param name="azureSearchKey">A key of Azure AI search.</param>
    /// <param name="azureSearchEndpoint">An endpoint of Azure AI search.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static SearchIndexClient InitializeSearchIndexClient(string azureSearchKey, string azureSearchEndpoint)
    {
        if (string.IsNullOrWhiteSpace(azureSearchKey)) 
            throw new ArgumentNullException(nameof(azureSearchKey), "Search API Key not found"); 

        if (string.IsNullOrWhiteSpace(azureSearchEndpoint)) 
            throw new ArgumentNullException( nameof(azureSearchEndpoint), "Search Endpoint not found"); 

        return new SearchIndexClient(new Uri(azureSearchEndpoint), new AzureKeyCredential(azureSearchKey));
    }

    internal async Task SetupIndexAsync(SearchIndexClient indexClient)
    {
        const string vectorSearchHnswProfile = "my-vector-profile";
        const string vectorSearchHnswConfig = "myHnsw";
        const string vectorSearchVectorizer = "myOpenAIVectorizer";
        const string semanticSearchConfig = "my-semantic-config";

        SearchIndex searchIndex = new(OfstedIndexName)
        {
            VectorSearch = new()
            {
                Profiles =
                {
                    new VectorSearchProfile(vectorSearchHnswProfile, vectorSearchHnswConfig)
                    {
                        VectorizerName = vectorSearchVectorizer
                    }
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(vectorSearchHnswConfig)
                },
                Vectorizers =
                {
                    new AzureOpenAIVectorizer(vectorSearchVectorizer)
                    {
                        Parameters = new AzureOpenAIVectorizerParameters
                        {
                            ResourceUri = new Uri(azureSettings.AzureOpenaiEndpoint),
                            ModelName = "text-embedding-ada-002",
                            DeploymentName = "text-embedding-ada-002"
                        }
                    }
                }
            },
            SemanticSearch = new()
            {
                Configurations =
                    {
                       new SemanticConfiguration(semanticSearchConfig, new()
                       {
                            TitleField = new SemanticField("title"),
                            ContentFields =
                            {
                                new SemanticField("content")
                            },
                            KeywordsFields =
                            {
                                new SemanticField("category")
                            }
                       })

                },
            },
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true, IsFacetable = true },
                new SearchableField("title") { IsFilterable = true, IsSortable = true },
                new SearchableField("content") { IsFilterable = true },
                new SearchField("titleVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = 1536,
                    VectorSearchProfileName = vectorSearchHnswProfile
                },
                new SearchField("contentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = 1536,
                    VectorSearchProfileName = vectorSearchHnswProfile
                },
                new SearchableField("category") { IsFilterable = true, IsSortable = true, IsFacetable = true }
            }
        };

        await indexClient.CreateOrUpdateIndexAsync(searchIndex);
    }

    internal async Task UploadSampleDocumentsAsync(SearchClient searchClient)
    {
        var sampleDocuments = await BuildDocumentsAsync();

        var options = new SearchIndexingBufferedSenderOptions<Dictionary<string, object>>
        {
            KeyFieldAccessor = (o) => o["id"].ToString()
        };
        using SearchIndexingBufferedSender<Dictionary<string, object>> bufferedSender = new(searchClient, options);
        await bufferedSender.UploadDocumentsAsync(sampleDocuments);
        await bufferedSender.FlushAsync();
    }

    /// <summary>
    /// Generates embeddings for sample documents and saves the output to a specified path.
    /// </summary>
    /// <param name="configuration">The configuration settings for the Azure OpenAI service.</param>
    /// <param name="azureOpenAIClient">The AzureOpenAIClient instance for embedding generation.</param>
    /// <param name="inputSampleDocumentPath">The file path of the input sample document containing JSON content.</param>
    /// <param name="outputSampleDocumentPath">The file path where the output with embeddings will be saved.</param>
    private async Task<List<Dictionary<string, object>>> BuildDocumentsAsync()
    {
        var azureOpenAiClient = azureOpenAIService.InitialiseAzureOpenAIClient(azureSettings.AzureOpenaiKey, azureSettings.AzureOpenaiEndpoint, true);

        EmbeddingClient embeddingClient = azureOpenAiClient.GetEmbeddingClient("text-embedding-ada-002");
        var embeddingOptions = new EmbeddingGenerationOptions();

        string sampleDocumentContent = await File.ReadAllTextAsync(Path.Join(Path.GetDirectoryName(AppContext.BaseDirectory), "\\Data\\ContentInspectionData100.json"));
        var sampleDocuments = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(sampleDocumentContent);


        foreach (Dictionary<string, object> sampleDocument in sampleDocuments!)
        {
            string title = sampleDocument["title"]?.ToString() ?? string.Empty;
            string content = sampleDocument["content"]?.ToString() ?? string.Empty;

            OpenAIEmbedding titleEmbedding = await embeddingClient.GenerateEmbeddingAsync(title, embeddingOptions);
            OpenAIEmbedding contentEmbedding = await embeddingClient.GenerateEmbeddingAsync(content, embeddingOptions);

            sampleDocument["titleVector"] = titleEmbedding.ToFloats();
            sampleDocument["contentVector"] = contentEmbedding.ToFloats();
        }

        return sampleDocuments;

    }
}
