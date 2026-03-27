using System.Text.Json;
using System.Text.Json.Serialization;

namespace BriefingTool.Services;

public interface IAcademyInformationRetriever
{
    string GetAcademyInformation(string academyName);

    object[] GetAllAcademies();
}


public class AcademyInformationRetriever: IAcademyInformationRetriever
{
    public string GetAcademyInformation(string academyName)
    {
        var academyData = File.ReadAllText(Path.Join(Path.GetDirectoryName(AppContext.BaseDirectory),  Path.Combine("Data", "InspectionData.json")));

        var database = JsonSerializer.Deserialize<dynamic[]>(academyData);

        foreach (var item in database)
        {
            if (string.Compare(item.GetProperty("School name").ToString(), academyName.ToLower(), StringComparison.OrdinalIgnoreCase) == 0)
            {
                return JsonSerializer.Serialize(item);
            }
        }

        throw new Exception($"Academy with name '{academyName}' not found in the database.");

    }

    public object[] GetAllAcademies()
    {
        var academyData = File.ReadAllText(Path.Join(Path.GetDirectoryName(AppContext.BaseDirectory), "\\Data\\IndexInspectionData.json"));

        var database = JsonSerializer.Deserialize<object[]>(academyData);

        return database;
    }
}