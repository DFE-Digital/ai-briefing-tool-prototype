namespace BriefingTool.Services
{
    public interface IConcernsInformationRetriever
    {
        public string GetTrustConcerns();
    }

    public class ConcernsInformationRetriever: IConcernsInformationRetriever
    {
        public string GetTrustConcerns()
        {
            string basePrompt = File.ReadAllText(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), @"\Data\ConcernsTextify.txt"));

            //var academyData = File.ReadAllText(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), "\\Data\\ConcernsData.json"));

            return basePrompt;
        }
    }
}
