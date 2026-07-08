namespace ParliamentOffice.UI.Services.AchievementEngine;

public class AchievementCrossCheckResult
{
    public int AchievementId { get; set; }
    public string AchievementTitle { get; set; } = string.Empty;
    public int LinkedOutgoingCount { get; set; }
    public int LinkedIncomingCount { get; set; }
    public int EvidenceCount { get; set; }
    public int AttachmentCount { get; set; }
    public int BeneficiariesCount { get; set; }
    public int ReliabilityScore { get; set; }
    public string ReliabilityStatus { get; set; } = string.Empty;
    public bool HasPossibleDuplicate { get; set; }
    public List<string> MissingItems { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public bool CanBeIncludedInFinalReport => ReliabilityScore >= 60 && MissingItems.Count == 0;
}
