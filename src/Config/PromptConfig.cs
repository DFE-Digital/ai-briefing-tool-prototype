using BriefingTool.Enums;

namespace BriefingTool.Config;

public class PromptConfig
{
    public Dictionary<SystemPromptType, string> SystemPrompts { get; set; } = [];
    public Dictionary<UserPromptType, string> UserPrompts { get; set; } = [];
}
