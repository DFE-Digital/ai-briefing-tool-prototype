namespace BriefingTool.Models;

public record BriefingParameters(string AcademyName, bool Ofsted, bool OfstedSummary, bool Concerns, bool Financial, string? AdditionalPrompt, string? UploadFileContents);
