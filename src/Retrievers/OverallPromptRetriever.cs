using BriefingTool.Retrievers.Interfaces;

namespace BriefingTool.Retrievers;

public class OverallPromptRetriever() : FileLoadRetriever(Path.Combine("Prompts", "OverallSummary.txt")), IOverallPromptRetriever;
