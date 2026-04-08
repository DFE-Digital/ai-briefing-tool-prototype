using BriefingTool.Retrievers.Interfaces;

namespace BriefingTool.Retrievers;

public class BasePromptRetriever() : FileLoadRetriever(Path.Combine("Prompts", "Base.txt")), IBasePromptRetriever;
