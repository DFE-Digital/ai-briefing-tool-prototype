using BriefingTool.Retrievers.Interfaces;

namespace BriefingTool.Retrievers;

public class ConcernsPromptRetriever() : FileLoadRetriever(Path.Combine("Prompts", "Concerns.txt")), IConcernsPromptRetriever;
