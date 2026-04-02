using OpenAI.Chat;

namespace BriefingTool.Builders.Interfaces;

public interface IPromptBuilder
{
    IEnumerable<ChatMessage> GetMessages();
    string GetPrompt();
    void AddSystemMessage(string prompt);
    void AddUserMessage(string prompt);
}
