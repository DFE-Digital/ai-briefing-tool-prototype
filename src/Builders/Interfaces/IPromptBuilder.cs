using Anthropic.Models.Messages;
using OpenAI.Chat;

namespace BriefingTool.Builders.Interfaces;

public interface IPromptBuilder
{
    IEnumerable<ChatMessage> GetMessages();
    IEnumerable<MessageParam> GetAnthropicMessages();
    string GetPrompt();
    void AddSystemMessage(string prompt);
    void AddUserMessage(string prompt);
    void AddAnthropicUserMessage(string message);
}
