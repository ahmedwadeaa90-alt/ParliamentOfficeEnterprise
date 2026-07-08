using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace ParliamentOffice.UI.Services.AchievementEngine
{
    internal class WordReaderService
    {
    }
}
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ParliamentOffice.UI.Services.AchievementEngine
{
    public class WordReaderService
    {
        public string ReadPlainText(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return string.Empty;

            using var wordDoc = WordprocessingDocument.Open(filePath, false);
            var body = wordDoc.MainDocumentPart?.Document.Body;

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
}