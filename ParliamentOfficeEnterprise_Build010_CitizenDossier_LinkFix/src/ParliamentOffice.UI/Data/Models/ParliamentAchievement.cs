namespace ParliamentOffice.UI.Data.Models;

public class ParliamentAchievement
{
    public int Id { get; set; }
    public string AchievementCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string AchievementType { get; set; } = string.Empty;
    public string Governorate { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string SubDistrict { get; set; } = string.Empty;
    public string ResponsibleOrganization { get; set; } = string.Empty;
    public string ProblemStatement { get; set; } = string.Empty;
    public string OfficeAction { get; set; } = string.Empty;
    public string ResultSummary { get; set; } = string.Empty;
    public string PublicImpact { get; set; } = string.Empty;
    public int BeneficiariesCount { get; set; }
    public decimal? EstimatedCost { get; set; }
    public int ProgressPercent { get; set; }
    public string Status { get; set; } = "قيد المتابعة";
    public string ReliabilityStatus { get; set; } = "غير مدقق";
    public int ReliabilityScore { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
