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
        IConcernsInformationRetriever concernsInformationRetriever) : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "State if the conversion is due to 2RI. Choose yes or no")]
        [Display(Name = "Result")]
        public string? AcademyName { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "State if the conversion is due to 2RI. Choose yes or no")]
        [Display(Name = "Result")]
        public string? Result { get; set; }

        public int? TotalTokens { get; set; }

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPostAsync()
        {
            //Result = basePromptRetriever.GetBasePrompt();
            Result = await RunAsync();

            return Page();
        }

        public async Task<string> RunAsync()
        {
            // Retrieve the OpenAI endpoint from environment variables
            var endpoint = GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "https://uc021-openai-sandbox-uks.openai.azure.com/";
            if (string.IsNullOrEmpty(endpoint))
            {

                return "Please set the AZURE_OPENAI_ENDPOINT environment variable.";
            }

            var key = configuration["AZURE_OPENAI_KEY"];
            if (string.IsNullOrEmpty(key))
            {

                return "Please set the AZURE_OPENAI_KEY environment variable.";
            }

            if (string.IsNullOrEmpty(AcademyName))
            {

                return "Enter an academy name";
            }

            AzureKeyCredential credential = new AzureKeyCredential(key);

            // Initialize the AzureOpenAIClient
            AzureOpenAIClient azureClient = new(new Uri(endpoint), credential);

            // Initialize the ChatClient with the specified deployment name
            ChatClient chatClient = azureClient.GetChatClient("UC021-gpt-4o");

            var academyData = academyInformationRetriever.GetAcademyInformation(AcademyName);

            var concernsData = concernsInformationRetriever.GetTrustConcerns();

            var jsonAcademyData = JsonSerializer.Serialize(academyData);

            // List of messages to send
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(basePromptRetriever.GetPrompt()),
                new SystemChatMessage(@$"Here is ofsted inspection data associated with {AcademyName} in JSON format: {jsonAcademyData}"),
                new SystemChatMessage(concernsPromptRetriever.GetPrompt()),
                new SystemChatMessage(@$"Here are concerns related to the trust for this academy in the last 3 years associated with {AcademyName}: {concernsData}"),
                new UserChatMessage(@$"Create a briefing for {AcademyName}"),
                new AssistantChatMessage(@""),
            };

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
                ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);

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

                    return chatResult.ToString();
                }
                else
                {
                    return "No response received.";
                }
            }
            catch (Exception ex)
            {
                return $"An error occurred: {ex.Message}";
            }
        }
    }
}
