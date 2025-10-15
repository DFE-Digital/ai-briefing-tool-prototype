namespace BriefingTool.Services
{
    public interface IBasePromptRetriever : IPromptRetriever;

    public class BasePromptRetriever() : FileLoadRetriever("\\Prompts\\Base.txt"), IBasePromptRetriever;

}
