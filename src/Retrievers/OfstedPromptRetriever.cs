using BriefingTool.Retrievers.Interfaces;

namespace BriefingTool.Retrievers;


public class OfstedPromptRetriever() : FileLoadRetriever(Path.Combine("Prompts", "Ofsted.txt")), IOfstedPromptRetriever; 

public class OfstedSummaryPromptRetriever() : FileLoadRetriever(Path.Combine("Prompts", "OfstedSummary.txt")), IOfstedSummaryPromptRetriever;
