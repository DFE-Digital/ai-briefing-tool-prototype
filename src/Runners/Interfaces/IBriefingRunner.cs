using BriefingTool.Models;

namespace BriefingTool.Runners.Interfaces;

public interface IBriefingRunner
{
    Task<AIResult> GetBriefing(BriefingParameters briefing);
}


public interface ISingleSourceBriefingRunner
{
    Task<AIResult> GetBriefing(BriefingParameters briefing);
}