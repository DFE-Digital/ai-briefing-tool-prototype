using Clippit.Word;
using DocumentFormat.OpenXml.Packaging; 

namespace BriefingTool.Converter;

public static class WordToHtmlConverter
{
    /// <summary>
    /// Converts a DOCX file stream to an HTML string using Open XML SDK and Clippit.Word library.
    /// </summary>
    /// <param name="docxStream"></param>
    /// <returns></returns>
    public static string ConvertDocxToHtml(Stream docxStream)
    {
        using var memoryStream = new MemoryStream();
        docxStream.CopyTo(memoryStream);

        using var wordDoc = WordprocessingDocument.Open(memoryStream, true);
        var settings = new WmlToHtmlConverterSettings
        {
            PageTitle = "Converted Document"
        };
         
        var html = WmlToHtmlConverter.ConvertToHtml(wordDoc, settings);
         
        string htmlText = html.ToString();
        return htmlText; 
    }

    /// <summary>
    /// Converts an HTML string to Markdown format using the ReverseMarkdown library.
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    public static string HtmlToMarkdown(string html)
    {
        var converter = new ReverseMarkdown.Converter();
        var markdown = converter.Convert(html);
        return markdown;
    }
}
