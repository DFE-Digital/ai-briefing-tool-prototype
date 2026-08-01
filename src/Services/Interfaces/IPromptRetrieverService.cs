using BriefingTool.Enums;

namespace BriefingTool.Services.Interfaces;

public interface IPromptRetrieverService
{
    string GetSystemPrompt(SystemPromptType promptType);
    string GetUserPrompt(UserPromptType promptType);
    string Render(string template, Dictionary<string, string> values);
}
