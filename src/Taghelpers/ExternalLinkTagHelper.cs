using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BriefingTool.Taghelpers;

/// <summary>
/// Tag helper for creating secure external links that open in new tabs
/// Automatically adds rel="noopener noreferrer" and target="_blank" for security
/// </summary>
[HtmlTargetElement("external-link")]
public class ExternalLinkTagHelper : TagHelper
{
    /// <summary>
    /// The URL to link to
    /// </summary>
    [HtmlAttributeName("href")]
    public string? Href { get; set; }

    /// <summary>
    /// Additional CSS classes to apply to the link
    /// </summary>
    [HtmlAttributeName("class")]
    public string? CssClass { get; set; }

    /// <summary>
    /// Whether to add the "(opens in new tab)" text automatically
    /// Default is true
    /// </summary>
    [HtmlAttributeName("show-new-tab-text")]
    public bool ShowNewTabText { get; set; } = true;

    /// <summary>
    /// Custom accessible description for screen readers
    /// If not provided, will use default "(opens in new tab)" text
    /// </summary>
    [HtmlAttributeName("sr-description")]
    public string? ScreenReaderDescription { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(Href))
        {
            output.SuppressOutput();
            return;
        }

        // Change the tag to an anchor
        output.TagName = "a";

        // Set the href attribute
        output.Attributes.SetAttribute("href", Href);

        // Always add security attributes for external links
        output.Attributes.SetAttribute("target", "_blank");
        output.Attributes.SetAttribute("rel", "noopener noreferrer");

        if (!string.IsNullOrEmpty(CssClass))
            output.Attributes.SetAttribute("class", CssClass);

        var existingContent = output.GetChildContentAsync().Result.GetContent();

        if (ShowNewTabText)
        {
            var newTabText = !string.IsNullOrEmpty(ScreenReaderDescription)
                ? ScreenReaderDescription
                : "(opens in new tab)";

            if (!existingContent.Contains("opens in", StringComparison.OrdinalIgnoreCase))
                output.Content.SetHtmlContent($"{existingContent} {newTabText}");
            else
                output.Content.SetHtmlContent(existingContent);
        }
        else
        {
            output.Content.SetHtmlContent(existingContent);
        }
    }
}
