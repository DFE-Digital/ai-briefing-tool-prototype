using System.Text.Json;
using System.Text.Json.Serialization;

namespace BriefingTool.Services;

public class AcademyInformation
{
    [JsonPropertyName("URN")]
    public string URN { get; set; }

    [JsonPropertyName("LAESTAB")]
    public string LAESTAB { get; set; }

    [JsonPropertyName("School name")]
    public string SchoolName { get; set; }

    [JsonPropertyName("Ofsted phase")]
    public string OfstedPhase { get; set; }

    [JsonPropertyName("Type of education")]
    public string TypeOfEducation { get; set; }

    [JsonPropertyName("School open date")]
    public string? SchoolOpenDate { get; set; }

    [JsonPropertyName("Admissions policy")]
    public string AdmissionsPolicy { get; set; }

    [JsonPropertyName("Sixth form")]
    public string SixthForm { get; set; }

    [JsonPropertyName("Designated religious character")]
    public string DesignatedReligiousCharacter { get; set; }

    [JsonPropertyName("Religious ethos")]
    public string ReligiousEthos { get; set; }

    [JsonPropertyName("Faith grouping")]
    public string FaithGrouping { get; set; }

    [JsonPropertyName("Ofsted region")]
    public string OfstedRegion { get; set; }

    [JsonPropertyName("Region")]
    public string Region { get; set; }

    [JsonPropertyName("Local authority")]
    public string LocalAuthority { get; set; }

    [JsonPropertyName("Parliamentary constituency")]
    public string ParliamentaryConstituency { get; set; }

    [JsonPropertyName("Multi-academy trust UID")]
    public string MultiAcademyTrustUID { get; set; }

    [JsonPropertyName("Multi-academy trust name")]
    public string MultiAcademyTrustName { get; set; }

    [JsonPropertyName("Academy sponsor UID")]
    public string AcademySponsorUID { get; set; }

    [JsonPropertyName("Academy sponsor name")]
    public string AcademySponsorName { get; set; }

    [JsonPropertyName("Postcode")]
    public string Postcode { get; set; }

    [JsonPropertyName("The income deprivation affecting children index (IDACI) quintile")]
    public int? IDACIQuintile { get; set; }

    [JsonPropertyName("Total number of pupils")]
    public int? TotalNumberOfPupils { get; set; }

    [JsonPropertyName("Statutory lowest age")]
    public int? StatutoryLowestAge { get; set; }

    [JsonPropertyName("Statutory highest age")]
    public int? StatutoryHighestAge { get; set; }

    [JsonPropertyName("Inspection number")]
    public string InspectionNumber { get; set; }

    [JsonPropertyName("Inspection type")]
    public string InspectionType { get; set; }

    [JsonPropertyName("Inspection type grouping")]
    public string InspectionTypeGrouping { get; set; }

    [JsonPropertyName("Event type grouping")]
    public string EventTypeGrouping { get; set; }

    [JsonPropertyName("Inspection start date")]
    public string? InspectionStartDate { get; set; }

    [JsonPropertyName("Publication date")]
    public string? PublicationDate { get; set; }

    [JsonPropertyName("Did the latest ungraded inspection convert to a graded inspection?")]
    public string ConvertedToGradedInspection { get; set; }

    [JsonPropertyName("Outcomes for ungraded and monitoring inspections")]
    public string OutcomesForUngradedAndMonitoringInspections { get; set; }

    [JsonPropertyName("Category of concern")]
    public string CategoryOfConcern { get; set; }

    [JsonPropertyName("Quality of education")]
    public int? QualityOfEducation { get; set; }

    [JsonPropertyName("Behaviour and attitudes")]
    public int? BehaviourAndAttitudes { get; set; }

    [JsonPropertyName("Personal development")]
    public int? PersonalDevelopment { get; set; }

    [JsonPropertyName("Effectiveness of leadership and management")]
    public int? EffectivenessOfLeadershipAndManagement { get; set; }

    [JsonPropertyName("Safeguarding is effective")]
    public bool? SafeguardingIsEffective { get; set; }

    [JsonPropertyName("Early years provision (where applicable)")]
    public int? EarlyYearsProvision { get; set; }

    [JsonPropertyName("Sixth form provision (where applicable)")]
    public int? SixthFormProvision { get; set; }

    // --- Previous inspection details ---

    [JsonPropertyName("Previous inspection number")]
    public string PreviousInspectionNumber { get; set; }

    [JsonPropertyName("Previous inspection start date")]
    public string? PreviousInspectionStartDate { get; set; }

    [JsonPropertyName("Previous publication date")]
    public string? PreviousPublicationDate { get; set; }

    [JsonPropertyName("Does the previous inspection relate to the school in its current form?")]
    public bool? PreviousInspectionRelatesToCurrentForm { get; set; }

    [JsonPropertyName("URN at time of previous inspection")]
    public string URNAtPreviousInspection { get; set; }

    [JsonPropertyName("LAESTAB at time of previous inspection")]
    public string LAESTABAtPreviousInspection { get; set; }

    [JsonPropertyName("School name at time of previous inspection")]
    public string SchoolNameAtPreviousInspection { get; set; }

    [JsonPropertyName("School type at time of previous inspection")]
    public string SchoolTypeAtPreviousInspection { get; set; }

    [JsonPropertyName("Previous overall effectiveness")]
    public int? PreviousOverallEffectiveness { get; set; }

    [JsonPropertyName("Previous category of concern")]
    public string PreviousCategoryOfConcern { get; set; }

    [JsonPropertyName("Previous quality of education")]
    public int? PreviousQualityOfEducation { get; set; }

    [JsonPropertyName("Previous behaviour and attitudes")]
    public int? PreviousBehaviourAndAttitudes { get; set; }

    [JsonPropertyName("Previous personal development")]
    public int? PreviousPersonalDevelopment { get; set; }

    [JsonPropertyName("Previous effectiveness of leadership and management")]
    public int? PreviousEffectivenessOfLeadershipAndManagement { get; set; }

    [JsonPropertyName("Previous safeguarding is effective?")]
    public int? PreviousSafeguardingIsEffective { get; set; }

    [JsonPropertyName("Previous early years provision (where applicable)")]
    public int? PreviousEarlyYearsProvision { get; set; }

    [JsonPropertyName("Previous sixth form provision (where applicable)")]
    public int? PreviousSixthFormProvision { get; set; }
}

public interface IAcademyInformationRetriever
{
    string GetAcademyInformation(string academyName);
}


public class AcademyInformationRetriever: IAcademyInformationRetriever
{
    public string GetAcademyInformation(string academyName)
    {
        var academyData = File.ReadAllText(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), "\\Data\\InspectionData.json"));

        var database = JsonSerializer.Deserialize<dynamic[]>(academyData);

        foreach (var item in database)
        {
            if (string.Compare(item.GetProperty("School name").ToString(), academyName.ToLower(), StringComparison.OrdinalIgnoreCase) == 0)
            {
                return JsonSerializer.Serialize(item);
            }
        }

        //var academyInfo = database.Find(x => x.SchoolName == academyName);

        //if (academyInfo == null)
        //{
            throw new Exception($"Academy with name '{academyName}' not found in the database.");
        //}
        //var school = new AcademyInformation
        //{
        //    URN = "139439",
        //    LAESTAB = "3302126",
        //    SchoolName = "Tiverton Academy",
        //    OfstedPhase = "Primary",
        //    TypeOfEducation = "Academy Sponsor Led",
        //    SchoolOpenDate = DateTime.Parse("01/05/2013"),
        //    AdmissionsPolicy = "Not applicable",
        //    SixthForm = "Does not have a sixth form",
        //    DesignatedReligiousCharacter = "Does not apply",
        //    ReligiousEthos = "None",
        //    FaithGrouping = "Non-faith",
        //    OfstedRegion = "West Midlands",
        //    Region = "West Midlands",
        //    LocalAuthority = "Birmingham",
        //    ParliamentaryConstituency = "Birmingham Selly Oak",
        //    MultiAcademyTrustUID = "3025",
        //    MultiAcademyTrustName = "The Elliot Foundation Academies Trust",
        //    AcademySponsorUID = "3024",
        //    AcademySponsorName = "The Elliot Foundation Academies Trust",
        //    Postcode = "B29 6BW",
        //    IDACIQuintile = 5,
        //    TotalNumberOfPupils = 200,
        //    StatutoryLowestAge = 4,
        //    StatutoryHighestAge = 11,
        //    InspectionNumber = "10344003",
        //    InspectionType = "S5 Inspection",
        //    InspectionTypeGrouping = "S5 Inspection",
        //    EventTypeGrouping = "Schools - S5",
        //    InspectionStartDate = DateTime.Parse("24/09/2024"),
        //    PublicationDate = DateTime.Parse("10/11/2024"),
        //    ConvertedToGradedInspection = "",
        //    OutcomesForUngradedAndMonitoringInspections = "",
        //    CategoryOfConcern = "",
        //    QualityOfEducation = 2,
        //    BehaviourAndAttitudes = 2,
        //    PersonalDevelopment = 2,
        //    EffectivenessOfLeadershipAndManagement = 2,
        //    SafeguardingIsEffective = true,
        //    EarlyYearsProvision = 2,
        //    SixthFormProvision = 9,
        //    PreviousInspectionNumber = "ITS449916",
        //    PreviousInspectionStartDate = DateTime.Parse("24/03/2015"),
        //    PreviousPublicationDate = DateTime.Parse("23/04/2015"),
        //    PreviousInspectionRelatesToCurrentForm = true,
        //    URNAtPreviousInspection = "139439",
        //    LAESTABAtPreviousInspection = "3302126",
        //    SchoolNameAtPreviousInspection = "Tiverton Academy",
        //    SchoolTypeAtPreviousInspection = "Academy Sponsor Led",
        //    PreviousOverallEffectiveness = 1,
        //    PreviousCategoryOfConcern = "",
        //    PreviousQualityOfEducation = 9,
        //    PreviousBehaviourAndAttitudes = 9,
        //    PreviousPersonalDevelopment = 9,
        //    PreviousEffectivenessOfLeadershipAndManagement = 1,
        //    PreviousSafeguardingIsEffective = 9,
        //    PreviousEarlyYearsProvision = 1,
        //    PreviousSixthFormProvision = 9
        //};

    }
}