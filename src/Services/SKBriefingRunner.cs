using Azure;
using BriefingTool.Config;
using Markdig;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using OpenAI.Chat;

namespace BriefingTool.Services
{
    public class SKBriefingRunner(
        IOptions<AzureSettings> settings,
        ILogger<SKBriefingRunner> logger) : IBriefingRunner 
    {
        public async Task<AIResult> GetBriefing(BriefingParameters briefing)
        {
            var azureSettings = settings.Value;

            if (string.IsNullOrEmpty(briefing.AcademyName))
            {
                return new AIResult("", "Enter an academy name", -1);
            }

            // Create chat completion options
            var options = new ChatCompletionOptions
            {
                Temperature = (float)0.7,
                MaxOutputTokenCount = 6553,

                TopP = (float)0.95,
                FrequencyPenalty = (float)0,
                PresencePenalty = (float)0,
            };

            logger.LogInformation($"AI endpoint: {azureSettings.AzureOpenaiEndpoint}");
            
            // Create the Semantic Kernel instance
            var builder = Kernel.CreateBuilder();

            // Add Azure OpenAI Chat Completion service
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: azureSettings.AzureOpenaiDeployment,
                endpoint: azureSettings.AzureOpenaiEndpoint,
                apiKey: azureSettings.AzureOpenaiKey
            );

            builder.AddAzureOpenAITextEmbeddingGeneration(
                deploymentName: azureSettings.AzureEmbeddingDeployment,
                endpoint: azureSettings.AzureEmbeddingEndpoint,
                apiKey: azureSettings.AzureEmbeddingKey
            );

            builder.Services
                .AddAzureAISearchVectorStore(new Uri(azureSettings.AzureSearchEndpoint), new AzureKeyCredential(azureSettings.AzureSearchKey));
            
            var kernel = builder.Build();
            


            // Create a simple prompt function
            var prompt = kernel.CreateFunctionFromPrompt(
                "You are a helpful assistant. Answer clearly: {{$input}}"
            );

            // Run the function with user input
            Console.Write("Enter your question: ");
            string userInput = $"Can you create a briefing for the UK school {briefing.AcademyName}";

            if (string.IsNullOrWhiteSpace(userInput))
            {
                return new AIResult("", "No input provided.", -1);
            }

            var result = await kernel.InvokeAsync(prompt, new() { ["input"] = userInput });
            string html = Markdown.ToHtml(result.GetValue<string>() ?? "");
            return new AIResult(html, "", 1);
        }
    }
}
