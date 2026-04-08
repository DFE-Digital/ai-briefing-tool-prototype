namespace BriefingTool.Retrievers.Interfaces;
public interface IAcademyInformationRetriever
{
    string GetAcademyInformation(string academyName); 
    object[] GetAllAcademies();
}
