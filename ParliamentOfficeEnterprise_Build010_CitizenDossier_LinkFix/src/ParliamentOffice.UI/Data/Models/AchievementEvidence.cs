namespace ParliamentOffice.UI.Data.Models;

public class AchievementEvidence
{
    public int Id { get; set; }
    public int AchievementId { get; set; }
    public string EvidenceType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime? ReferenceDate { get; set; }
    public string SourceOrganization { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int Weight { get; set; } = 10;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
