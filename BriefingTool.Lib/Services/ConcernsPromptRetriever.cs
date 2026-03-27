namespace BriefingTool.Services
{
    public interface IConcernsPromptRetriever : IPromptRetriever;

    public class ConcernsPromptRetriever() : FileLoadRetriever(Path.Combine("Prompts", "Concerns.txt")), IConcernsPromptRetriever;
}
