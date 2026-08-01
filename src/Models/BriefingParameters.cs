using BriefingTool.Constants;

namespace BriefingTool.Models;

public record BriefingParameters(string AcademyName, bool Ofsted, bool OfstedSummary, bool Concerns, bool Financial, string? AdditionalPrompt, string? UploadFileContents, string RunnerServiceType = RunnerServiceType.SingleDataSource);
public record DatabricksQueryBriefingParameters(string Prompt);
public record OpenAiBriefingParameters(string AcademyName, bool Ofsted, bool OfstedSummary, bool Concerns, bool Financial, string? AdditionalPrompt, string? UploadFileContents);