using BriefingTool.Models;

namespace BriefingTool.Runners.Interfaces;

public interface IBriefingRunner
{
    Task<AIResult> GetBriefing(BriefingParameters briefing);
}
public interface IOpenAIAgentBriefingRunner
{
    Task<AIResult> GetBriefing(BriefingParameters briefing);
}
public interface IDatabricksQueryBriefingRunner
{
    Task<AIResult> GetBriefing(DatabricksQueryBriefingParameters briefing);
}
