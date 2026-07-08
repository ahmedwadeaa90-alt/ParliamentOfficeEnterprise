using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ParliamentOffice.UI.Data.Models;

namespace ParliamentOffice.UI.Services.AchievementEngine;

public class AchievementWordReportBuilder
{
    private readonly AchievementCrossCheckService _crossCheckService = new();

    public void BuildWordReport(
        string outputPath,
        IEnumerable<ParliamentAchievement> achievements,
        IDictionary<int, List<AchievementEvidence>> evidenceByAchievement,
        IDictionary<int, int> linkedOutgoingCounts,
        IDictionary<int, int> linkedIncomingCounts,
        IDictionary<int, int> attachmentCounts)
    {
        var list = achievements.ToList();
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using var document = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        var body = mainPart.Document.Body!;

        AddTitle(body, "تقرير منجز النائب الشيخ رياض عباس التميمي");
        AddParagraph(body, "تقرير Word قابل للتعديل مولد من Parliament Office Enterprise V11.");
        AddParagraph(body, $"تاريخ التوليد: {DateTime.Now:yyyy-MM-dd HH:mm}");

        var totalBeneficiaries = list.Sum(x => x.BeneficiariesCount);
        var sectors = list.Select(x => x.Sector).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Count();
        var completed = list.Count(x => x.Status.Contains("منجز") || x.ProgressPercent >= 100);

        AddHeading(body, "الملخص التنفيذي");
        AddParagraph(body, $"بلغ عدد المنجزات المسجلة ({list.Count}) منجزاً، منها ({completed}) منجز مكتمل، وبلغ عدد المستفيدين المثبتين ({totalBeneficiaries:N0}) مستفيداً، ضمن ({sectors}) قطاعات.");

        AddHeading(body, "مؤشرات الأداء");
        AddTable(body, new[] { "المؤشر", "القيمة" }, new[]
        {
            new[] { "إجمالي المنجزات", list.Count.ToString() },
            new[] { "المنجزات المكتملة", completed.ToString() },
            new[] { "عدد المستفيدين", totalBeneficiaries.ToString("N0") },
            new[] { "عدد القطاعات", sectors.ToString() }
        });

        AddHeading(body, "مصفوفة المنجزات والمقاطعة");
        var rows = new List<string[]>();
        foreach (var item in list)
        {
            evidenceByAchievement.TryGetValue(item.Id, out var evidence);
            linkedOutgoingCounts.TryGetValue(item.Id, out var outgoingCount);
            linkedIncomingCounts.TryGetValue(item.Id, out var incomingCount);
            attachmentCounts.TryGetValue(item.Id, out var attachCount);

            var check = _crossCheckService.Check(item, list, evidence ?? new List<AchievementEvidence>(), outgoingCount, incomingCount, attachCount);
            rows.Add(new[]
            {
                item.Title,
                item.Sector,
                item.ResponsibleOrganization,
                item.BeneficiariesCount.ToString("N0"),
                check.ReliabilityScore + "% - " + check.ReliabilityStatus,
                check.MissingItems.Count == 0 ? "مكتمل" : string.Join("، ", check.MissingItems),
                check.Warnings.Count == 0 ? "لا توجد" : string.Join("، ", check.Warnings)
            });
        }

        AddTable(body,
            new[] { "المنجز", "القطاع", "الجهة", "المستفيدون", "التوثيق", "النواقص", "التنبيهات" },
            rows);

        AddHeading(body, "قائمة التدقيق قبل اعتماد التقرير");
        AddParagraph(body, "لا يعتمد المنجز في التقرير النهائي إلا بعد مراجعة الحقول الأساسية، وربطه بالكتب أو الأدلة أو المرفقات المؤيدة، وفحص عدم تكراره مع منجز آخر.");

        mainPart.Document.Save();
    }

    private static void AddTitle(Body body, string text)
    {
        var p = new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
            new Run(new RunProperties(new Bold(), new FontSize { Val = "36" }), new Text(text)));
        body.Append(p);
    }

    private static void AddHeading(Body body, string text)
    {
        var p = new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
            new Run(new RunProperties(new Bold(), new FontSize { Val = "30" }), new Text(text)));
        body.Append(p);
    }

    private static void AddParagraph(Body body, string text)
    {
        var p = new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
            new Run(new RunProperties(new FontSize { Val = "24" }), new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        body.Append(p);
    }

    private static void AddTable(Body body, string[] headers, IEnumerable<string[]> rows)
    {
        var table = new Table();
        table.AppendChild(new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 6 },
                new BottomBorder { Val = BorderValues.Single, Size = 6 },
                new LeftBorder { Val = BorderValues.Single, Size = 6 },
                new RightBorder { Val = BorderValues.Single, Size = 6 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })));

        table.Append(CreateRow(headers, true));
        foreach (var row in rows)
        {
            table.Append(CreateRow(row, false));
        }
        body.Append(table);
    }

    private static TableRow CreateRow(IEnumerable<string> cells, bool header)
    {
        var tr = new TableRow();
        foreach (var cell in cells)
        {
            var props = header ? new RunProperties(new Bold()) : new RunProperties();
            tr.Append(new TableCell(
                new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Auto }),
                new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
                    new Run(props, new Text(cell ?? string.Empty)))));
        }
        return tr;
    }
}
