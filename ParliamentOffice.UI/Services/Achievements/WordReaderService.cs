using System.IO;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ParliamentOffice.UI.Services.Achievements;

public sealed class WordReaderService
{
    public string ReadPlainText(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return string.Empty;

        using var wordDoc = WordprocessingDocument.Open(filePath, false);
        var document = wordDoc.MainDocumentPart?.Document;
        var body = document?.Body;

        if (body == null)
            return string.Empty;

        var builder = new StringBuilder();

        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            var text = paragraph.InnerText?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                builder.AppendLine(text);
        }

        return builder.ToString().Trim();
    }

    public List<string> ReadParagraphs(string filePath)
    {
        var text = ReadPlainText(filePath);

        return text
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 2)
            .ToList();
    }

    public string ExtractTitle(string filePath)
    {
        var paragraphs = ReadParagraphs(filePath);
        return paragraphs.FirstOrDefault() ?? Path.GetFileNameWithoutExtension(filePath);
    }

    public string ExtractSummary(string filePath, int maxParagraphs = 4)
    {
        var paragraphs = ReadParagraphs(filePath);
        return string.Join(Environment.NewLine, paragraphs.Take(maxParagraphs));
    }
}
