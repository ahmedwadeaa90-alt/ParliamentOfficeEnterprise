using ParliamentOffice.UI.Data.Models;

namespace ParliamentOffice.UI.Services.AchievementEngine;

public class AchievementReliabilityService
{
    public int CalculateScore(ParliamentAchievement achievement, IEnumerable<AchievementEvidence> evidence)
    {
        var score = 0;

        if (!string.IsNullOrWhiteSpace(achievement.ProblemStatement)) score += 10;
        if (!string.IsNullOrWhiteSpace(achievement.OfficeAction)) score += 10;
        if (!string.IsNullOrWhiteSpace(achievement.ResultSummary)) score += 10;
        if (!string.IsNullOrWhiteSpace(achievement.PublicImpact)) score += 10;
        if (achievement.BeneficiariesCount > 0) score += 10;
        if (!string.IsNullOrWhiteSpace(achievement.ResponsibleOrganization)) score += 10;
        if (achievement.CompletedAt != null || achievement.ProgressPercent >= 80) score += 10;

        foreach (var item in evidence)
        {
            score += Math.Clamp(item.Weight, 0, 15);
        }

        return Math.Clamp(score, 0, 100);
    }

    public string GetReliabilityStatus(int score)
    {
        return score switch
        {
            >= 85 => "موثق بالكامل",
            >= 60 => "موثق جزئياً",
            >= 35 => "بحاجة إلى استكمال أدلة",
            _ => "غير موثق"
        };
    }
}
