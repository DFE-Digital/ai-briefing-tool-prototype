using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using BriefingTool.Services;
using OpenAI.Chat;

using static System.Environment;
using Markdig;


namespace BriefingTool.Pages
{
    public class IndexModel(
        ILogger<IndexModel> logger,
        IConfiguration configuration,
        IBasePromptRetriever basePromptRetriever,
        IConcernsPromptRetriever concernsPromptRetriever,
        IAcademyInformationRetriever academyInformationRetriever,
        IOfstedPromptRetriever ofstedPromptRetriever,
        IOfstedSummaryPromptRetriever ofstedSummaryPromptRetriever,
        IConcernsInformationRetriever concernsInformationRetriever) : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Enter an academy name")]
        [Display(Name = "AcademyName")]
        public string? AcademyName { get; set; }

        [BindProperty]
        [Display(Name = "Result")]
        public string? Result { get; set; }

        [BindProperty]
        [Display(Name = "Ofsted")]
        public bool Ofsted { get; set; }

        [BindProperty]
        [Display(Name = "Concerns")]
        public bool Concerns { get; set; }

        [BindProperty]
        [Display(Name = "Financial")]
        public bool Financial { get; set; }

        [BindProperty]
        [Display(Name = "Additional prompt information")]
        public string? AdditionalPrompt { get; set; }

        [BindProperty]
        [Display(Name = "Debug")]
        public bool DebugOutput { get; set; }

        [BindProperty]
        [Display(Name = "Debug information about prompt")]
        public string? DebugPrompt { get; set; }



        public int? TotalTokens { get; set; }

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPostAsync()
        {
            var output = await RunAsync();
            Result = output.output;
            DebugPrompt = output.debug;

            return Page();
        }

        public record AIResult(string output, string debug);

        public async Task<AIResult> RunAsync()
        {
            // Retrieve the OpenAI endpoint from environment variables
            var endpoint = GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "https://uc021-openai-sandbox-uks.openai.azure.com/";
            if (string.IsNullOrEmpty(endpoint))
            {

                return new AIResult("", "Please set the AZURE_OPENAI_ENDPOINT environment variable.");
            }

            var key = configuration["AZURE_OPENAI_KEY"];
            if (string.IsNullOrEmpty(key))
            {

                return new AIResult("", "Please set the AZURE_OPENAI_KEY environment variable.");
            }

            if (string.IsNullOrEmpty(AcademyName))
            {

                return new AIResult("", "Enter an academy name");
            }

            AzureKeyCredential credential = new AzureKeyCredential(key);

            // Initialize the AzureOpenAIClient
            AzureOpenAIClient azureClient = new(new Uri(endpoint), credential);

            // Initialize the ChatClient with the specified deployment name
            ChatClient chatClient = azureClient.GetChatClient("UC021-gpt-4o");

            var academyData = academyInformationRetriever.GetAcademyInformation(AcademyName);

            var concernsData = concernsInformationRetriever.GetTrustConcerns();

            var jsonAcademyData = JsonSerializer.Serialize(academyData);

            var promptBuilder = new PromptBuilder();
            
            promptBuilder.AddSystemMessage(basePromptRetriever.GetPrompt());

            if (Ofsted)
            {
                promptBuilder.AddSystemMessage(ofstedPromptRetriever.GetPrompt());
                promptBuilder.AddSystemMessage(ofstedSummaryPromptRetriever.GetPrompt());
                promptBuilder.AddSystemMessage(@$"Here is ofsted inspection data associated with {AcademyName} in JSON format: {jsonAcademyData}");
            }

            // List of messages to send
            if (Concerns)
            {
                promptBuilder.AddSystemMessage(concernsPromptRetriever.GetPrompt());
                promptBuilder.AddSystemMessage(
                    @$"Here are concerns related to the trust for this academy in the last 3 years associated with {AcademyName}: {concernsData}");
            }

            promptBuilder.AddUserMessage(@$"Create a briefing for {AcademyName}");

            if (!string.IsNullOrWhiteSpace(AdditionalPrompt))
            {
                promptBuilder.AddUserMessage(AdditionalPrompt);
            }
            
            // Create chat completion options

            var options = new ChatCompletionOptions
            {
                Temperature = (float)0.7,
                MaxOutputTokenCount = 6553,

                TopP = (float)0.95,
                FrequencyPenalty = (float)0,
                PresencePenalty = (float)0
            };

            try
            {
                TotalTokens = 0;
                // Create the chat completion request
                ChatCompletion completion = await chatClient.CompleteChatAsync(promptBuilder.GetMessages(), options);

                var chatResult = new StringBuilder();

                // Print the response
                if (completion != null)
                {
                    foreach (var content in completion.Content)
                    {
                        TotalTokens += completion.Usage.TotalTokenCount;
                        string html = Markdown.ToHtml(content.Text);

                        chatResult.Append(html);
                    }

                    return new AIResult(chatResult.ToString(), promptBuilder.GetPrompt());
                }

                return new AIResult("", "No response received.");
            }
            catch (Exception ex)
            {
                return new AIResult("", $"An error occurred: {ex.Message}");
            }
        }
    }
}
