namespace BriefingTool.Models;

public record BriefingParameters(string AcademyName, bool Ofsted, bool Concerns, bool Financial, string? AdditionalPrompt, string? UploadFileContents);
