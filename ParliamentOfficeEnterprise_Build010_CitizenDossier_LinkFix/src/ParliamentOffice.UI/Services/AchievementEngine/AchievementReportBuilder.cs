using System.Text;
using ParliamentOffice.UI.Data.Models;

namespace ParliamentOffice.UI.Services.AchievementEngine;

public class AchievementReportBuilder
{
    private readonly AchievementReliabilityService _reliabilityService = new();

    public string BuildExecutiveHtmlReport(IEnumerable<ParliamentAchievement> achievements, IDictionary<int, List<AchievementEvidence>> evidenceByAchievement)
    {
        var list = achievements.ToList();
        var totalBeneficiaries = list.Sum(x => x.BeneficiariesCount);
        var sectors = list.Select(x => x.Sector).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();
        var completed = list.Count(x => x.Status.Contains("منجز") || x.ProgressPercent >= 100);
        var inProgress = list.Count - completed;

        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html lang='ar' dir='rtl'><head><meta charset='utf-8'>");
        sb.AppendLine("<style>body{font-family:Segoe UI,Tahoma,Arial;line-height:1.9;color:#111827;margin:40px}h1,h2{color:#111827}table{width:100%;border-collapse:collapse;margin:12px 0}th,td{border:1px solid #d1d5db;padding:8px;text-align:right}th{background:#f3f4f6}.ok{color:#166534}.warn{color:#92400e}.bad{color:#991b1b}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<h1>تقرير منجز النائب الشيخ رياض عباس التميمي</h1>");
        sb.AppendLine("<p>تقرير مولد آلياً من منظومة Parliament Office Enterprise V11.</p>");

        sb.AppendLine("<h2>الملخص التنفيذي</h2>");
        sb.AppendLine($"<p>بلغ عدد المنجزات المسجلة ({list.Count}) منجزاً، منها ({completed}) منجز مكتمل و({inProgress}) ملف قيد المتابعة، وبلغ عدد المستفيدين المثبتين ({totalBeneficiaries:N0}) مستفيداً، ضمن ({sectors.Count}) قطاعات.</p>");

        sb.AppendLine("<h2>مؤشرات الأداء</h2><table><tr><th>المؤشر</th><th>القيمة</th></tr>");
        sb.AppendLine($"<tr><td>إجمالي المنجزات</td><td>{list.Count}</td></tr>");
        sb.AppendLine($"<tr><td>المنجزات المكتملة</td><td>{completed}</td></tr>");
        sb.AppendLine($"<tr><td>قيد المتابعة</td><td>{inProgress}</td></tr>");
        sb.AppendLine($"<tr><td>عدد المستفيدين</td><td>{totalBeneficiaries:N0}</td></tr>");
        sb.AppendLine($"<tr><td>القطاعات</td><td>{sectors.Count}</td></tr></table>");

        sb.AppendLine("<h2>المنجزات حسب القطاع</h2><table><tr><th>القطاع</th><th>عدد المنجزات</th><th>عدد المستفيدين</th></tr>");
        foreach (var sector in sectors)
        {
            var sectorItems = list.Where(x => x.Sector == sector).ToList();
            sb.AppendLine($"<tr><td>{Escape(sector)}</td><td>{sectorItems.Count}</td><td>{sectorItems.Sum(x => x.BeneficiariesCount):N0}</td></tr>");
        }
        sb.AppendLine("</table>");

        sb.AppendLine("<h2>مصفوفة المنجزات والتوثيق</h2><table><tr><th>المنجز</th><th>القطاع</th><th>الجهة</th><th>المستفيدون</th><th>الحالة</th><th>درجة التوثيق</th><th>الموقف الرقابي</th></tr>");
        foreach (var achievement in list)
        {
            evidenceByAchievement.TryGetValue(achievement.Id, out var evidence);
            evidence ??= new List<AchievementEvidence>();
            var score = _reliabilityService.CalculateScore(achievement, evidence);
            var status = _reliabilityService.GetReliabilityStatus(score);
            var cls = score >= 85 ? "ok" : score >= 60 ? "warn" : "bad";
            sb.AppendLine($"<tr><td>{Escape(achievement.Title)}</td><td>{Escape(achievement.Sector)}</td><td>{Escape(achievement.ResponsibleOrganization)}</td><td>{achievement.BeneficiariesCount:N0}</td><td>{Escape(achievement.Status)}</td><td class='{cls}'>{score}%</td><td>{Escape(status)}</td></tr>");
        }
        sb.AppendLine("</table>");

        sb.AppendLine("<h2>منهجية منع التكرار والإغفال</h2>");
        sb.AppendLine("<p>يعتمد التقرير على ربط كل منجز برقم تعريفي واحد، ثم ربط الكتب الصادرة والواردة والمرفقات والأدلة بذلك الرقم، وبذلك يتم تجنب تكرار الموضوع نفسه في أكثر من فصل، كما يتم كشف المنجزات الناقصة أو غير الموثقة قبل اعتماد التقرير النهائي.</p>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string Escape(string? text)
    {
        return System.Net.WebUtility.HtmlEncode(text ?? string.Empty);
    }
}
