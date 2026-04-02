using BriefingTool.Retrievers.Interfaces;
using System.Text.Json;

namespace BriefingTool.Retrievers;

public class AcademyInformationRetriever: IAcademyInformationRetriever
{
    public string GetAcademyInformation(string academyName)
    { 
        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "InspectionData.json");

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Inspection data file not found.", filePath);

        // Read file
        var academyData = File.ReadAllText(filePath);
         
        var database = JsonSerializer.Deserialize<JsonElement[]>(academyData) ?? [];

        // Find the first matching school
        var schoolInformation = database.FirstOrDefault(item =>
            item.TryGetProperty("School name", out var nameProp) && string.Equals(nameProp.GetString(), academyName, StringComparison.OrdinalIgnoreCase));

        if (schoolInformation.ValueKind != JsonValueKind.Undefined)
            return JsonSerializer.Serialize(schoolInformation);

        throw new KeyNotFoundException($"Academy with name '{academyName}' not found in the database.");
    }

    public object[] GetAllAcademies()
    {
        var academyData = File.ReadAllText(Path.Join(Path.GetDirectoryName(AppContext.BaseDirectory), "\\Data\\IndexInspectionData.json"));

        var database = JsonSerializer.Deserialize<object[]>(academyData);

        return database!;
    }
}