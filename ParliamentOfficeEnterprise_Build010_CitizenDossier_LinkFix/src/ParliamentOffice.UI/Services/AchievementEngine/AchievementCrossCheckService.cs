using ParliamentOffice.UI.Data.Models;

namespace ParliamentOffice.UI.Services.AchievementEngine;

public class AchievementCrossCheckService
{
    private readonly AchievementReliabilityService _reliabilityService = new();

    public AchievementCrossCheckResult Check(
        ParliamentAchievement achievement,
        IEnumerable<ParliamentAchievement> allAchievements,
        IEnumerable<AchievementEvidence> evidence,
        int linkedOutgoingCount,
        int linkedIncomingCount,
        int attachmentCount)
    {
        var evidenceList = evidence.ToList();
        var score = _reliabilityService.CalculateScore(achievement, evidenceList);
        var result = new AchievementCrossCheckResult
        {
            AchievementId = achievement.Id,
            AchievementTitle = achievement.Title,
            LinkedOutgoingCount = linkedOutgoingCount,
            LinkedIncomingCount = linkedIncomingCount,
            EvidenceCount = evidenceList.Count,
            AttachmentCount = attachmentCount,
            BeneficiariesCount = achievement.BeneficiariesCount,
            ReliabilityScore = score,
            ReliabilityStatus = _reliabilityService.GetReliabilityStatus(score),
            HasPossibleDuplicate = HasDuplicate(achievement, allAchievements)
        };

        if (string.IsNullOrWhiteSpace(achievement.Title)) result.MissingItems.Add("عنوان المنجز");
        if (string.IsNullOrWhiteSpace(achievement.Sector)) result.MissingItems.Add("القطاع");
        if (string.IsNullOrWhiteSpace(achievement.ResponsibleOrganization)) result.MissingItems.Add("الجهة المعنية");
        if (string.IsNullOrWhiteSpace(achievement.ProblemStatement)) result.MissingItems.Add("المشكلة أو سبب المتابعة");
        if (string.IsNullOrWhiteSpace(achievement.OfficeAction)) result.MissingItems.Add("إجراء المكتب");
        if (string.IsNullOrWhiteSpace(achievement.ResultSummary)) result.MissingItems.Add("النتيجة المتحققة");
        if (string.IsNullOrWhiteSpace(achievement.PublicImpact)) result.MissingItems.Add("الأثر على المواطنين");
        if (achievement.BeneficiariesCount <= 0) result.MissingItems.Add("عدد المستفيدين");

        if (linkedOutgoingCount == 0) result.Warnings.Add("لا يوجد كتاب صادر مرتبط بالمنجز");
        if (linkedIncomingCount == 0) result.Warnings.Add("لا يوجد كتاب وارد/جواب مرتبط بالمنجز");
        if (evidenceList.Count == 0 && attachmentCount == 0) result.MissingItems.Add("الأدلة أو المرفقات المؤيدة");
        if (achievement.ProgressPercent >= 100 && achievement.CompletedAt == null) result.Warnings.Add("المنجز مكتمل لكن لا يوجد تاريخ إنجاز مثبت");
        if (result.HasPossibleDuplicate) result.Warnings.Add("يوجد منجز مشابه وقد يكون مكرراً؛ راجع العنوان والقطاع والجهة");

        return result;
    }

    private static bool HasDuplicate(ParliamentAchievement item, IEnumerable<ParliamentAchievement> all)
    {
        var normalizedTitle = Normalize(item.Title);
        if (string.IsNullOrWhiteSpace(normalizedTitle)) return false;

        return all.Any(other =>
            other.Id != item.Id &&
            Normalize(other.Title) == normalizedTitle &&
            Normalize(other.Sector) == Normalize(item.Sector));
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty)
            .Replace("أ", "ا")
            .Replace("إ", "ا")
            .Replace("آ", "ا")
            .Replace("ة", "ه")
            .Replace("ى", "ي")
            .Trim()
            .ToLowerInvariant();
    }
}
