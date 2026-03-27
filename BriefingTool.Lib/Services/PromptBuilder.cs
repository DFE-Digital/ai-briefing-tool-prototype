using System.Text;
using OpenAI.Chat;

namespace BriefingTool.Services;

public class PromptBuilder
{
    private readonly List<ChatMessage> _messages = [];
    private readonly StringBuilder _promptBuilder = new();

    public IEnumerable<ChatMessage> GetMessages() => _messages;
    public string GetPrompt() => _promptBuilder.ToString();

    public void AddSystemMessage(string prompt)
    {
        _messages.Add(new SystemChatMessage(prompt));
        _promptBuilder.AppendLine(prompt);
    }
        
    public void AddUserMessage(string prompt)
    {
        _messages.Add(new UserChatMessage(prompt));
        _promptBuilder.AppendLine(prompt);
    }
}