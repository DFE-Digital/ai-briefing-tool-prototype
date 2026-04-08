using DocumentFormat.OpenXml.Packaging;
using OpenXmlPowerTools;
using Markdig;

namespace BriefingTool.Converter;

public static class WordToHtmlConverter
{
    public static string ConvertDocxToHtml(Stream docxStream)
    {
        using var memoryStream = new MemoryStream();
        docxStream.CopyTo(memoryStream);

        using var wordDoc = WordprocessingDocument.Open(memoryStream, true);
        var settings = new WmlToHtmlConverterSettings
        {
            PageTitle = "Converted Document"
        };

        // Convert Word → HTML first
        var html = WmlToHtmlConverter.ConvertToHtml(wordDoc, settings);

        // Extract HTML string
        string htmlText = html.ToString();
        return htmlText; 
    }

    public static string HtmlToMarkdown(string html)
    {
        var converter = new ReverseMarkdown.Converter();
        var markdown = converter.Convert(html);
        return markdown;
    }
}
