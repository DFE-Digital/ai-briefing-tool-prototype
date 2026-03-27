namespace BriefingTool.Services
{
    public interface IOverallPromptRetriever : IPromptRetriever;

    public class OverallPromptRetriever() : FileLoadRetriever(Path.Combine("Prompts", "OverallSummary.txt")), IOverallPromptRetriever;
}
