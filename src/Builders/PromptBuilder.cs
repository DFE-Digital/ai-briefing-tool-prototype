using Anthropic.Models.Messages;
using BriefingTool.Builders.Interfaces;
using OpenAI.Chat;
using System.Text;

namespace BriefingTool.Builders;

public class PromptBuilder: IPromptBuilder
{
    private readonly List<MessageParam> _anthropicMessages = [];
    private readonly List<ChatMessage> _messages = [];
    private readonly StringBuilder _promptBuilder = new();

    public IEnumerable<ChatMessage> GetMessages() => _messages;
    public IEnumerable<MessageParam> GetAnthropicMessages() => _anthropicMessages;
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
    public void AddAnthropicUserMessage(string message)
    {
        _anthropicMessages.Add(new MessageParam
        {
            Role = Role.User,
            Content = message
        });
    }
}