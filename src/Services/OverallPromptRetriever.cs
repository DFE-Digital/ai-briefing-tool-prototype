namespace BriefingTool.Services
{
    public interface IOverallPromptRetriever : IPromptRetriever;

    public class OverallPromptRetriever() : FileLoadRetriever("\\Prompts\\OverallSummary.txt"), IOverallPromptRetriever;
}
