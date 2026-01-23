namespace BriefingTool.Services
{
    public interface IBasePromptRetriever : IPromptRetriever;

    public class BasePromptRetriever() : FileLoadRetriever(Path.Combine("Prompts", "Base.txt")), IBasePromptRetriever;

}
