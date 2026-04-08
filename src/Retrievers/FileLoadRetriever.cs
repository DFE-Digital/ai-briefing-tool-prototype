using BriefingTool.Retrievers.Interfaces;

namespace BriefingTool.Retrievers;

public class FileLoadRetriever(string filename) : IPromptRetriever
{
    public string GetPrompt()
    {
        string basePrompt = File.ReadAllText(Path.Join(Path.GetDirectoryName(AppContext.BaseDirectory), filename));

        if (string.IsNullOrEmpty(basePrompt))
        {
            throw new Exception("Base prompt file is empty or not found.");
        }

        return basePrompt;
    }
}
