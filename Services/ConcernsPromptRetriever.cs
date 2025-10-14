namespace BriefingTool.Services
{
    public interface IConcernsPromptRetriever : IPromptRetriever;

    public class ConcernsPromptRetriever() : FileLoadRetriever("\\Prompts\\Concerns.txt"), IConcernsPromptRetriever;
}
