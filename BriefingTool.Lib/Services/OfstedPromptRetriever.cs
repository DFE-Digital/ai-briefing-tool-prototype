namespace BriefingTool.Services
{
    public interface IOfstedPromptRetriever : IPromptRetriever;

    public class OfstedPromptRetriever() : FileLoadRetriever(Path.Combine("Prompts", "Ofsted.txt")), IOfstedPromptRetriever;

    public interface IOfstedSummaryPromptRetriever : IPromptRetriever;

    public class OfstedSummaryPromptRetriever() : FileLoadRetriever(Path.Combine("Prompts", "OfstedSummary.txt")), IOfstedSummaryPromptRetriever;
}
