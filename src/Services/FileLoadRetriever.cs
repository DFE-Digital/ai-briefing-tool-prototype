namespace BriefingTool.Services
{
    public class FileLoadRetriever(string filename) : IPromptRetriever
    {
        private readonly string _filename = filename;

        public string GetPrompt()
        {
            string basePrompt = File.ReadAllText(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), filename));

            if (string.IsNullOrEmpty(basePrompt))
            {
                throw new Exception("Base prompt file is empty or not found.");
            }

            return basePrompt;
        }
    }
}
