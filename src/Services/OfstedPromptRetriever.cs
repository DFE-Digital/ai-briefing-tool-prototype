namespace BriefingTool.Services
{
    public interface IOfstedPromptRetriever : IPromptRetriever;

    public class OfstedPromptRetriever() : FileLoadRetriever("\\Prompts\\Ofsted.txt"), IOfstedPromptRetriever;

    public interface IOfstedSummaryPromptRetriever : IPromptRetriever;

    public class OfstedSummaryPromptRetriever() : FileLoadRetriever("\\Prompts\\OfstedSummary.txt"), IOfstedSummaryPromptRetriever;
}
