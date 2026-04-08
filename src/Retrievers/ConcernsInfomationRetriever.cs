using BriefingTool.Retrievers.Interfaces;

namespace BriefingTool.Retrievers;

public class ConcernsInformationRetriever: IConcernsInformationRetriever
{
    public string GetTrustConcerns()
    {
        string basePrompt = File.ReadAllText(Path.Join(Path.GetDirectoryName(AppContext.BaseDirectory),  Path.Combine("Data", "ConcernsTextify.txt")));
        
        return basePrompt;
    }
}
