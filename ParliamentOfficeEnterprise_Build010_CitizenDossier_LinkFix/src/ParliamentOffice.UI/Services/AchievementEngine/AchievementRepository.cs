using System.Data;
using Microsoft.Data.Sqlite;
using ParliamentOffice.UI.Data;
using ParliamentOffice.UI.Data.Models;

namespace ParliamentOffice.UI.Services.AchievementEngine;

public class AchievementRepository
{
    public int AddAchievement(ParliamentAchievement item, int? userId)
    {
        Db.Execute(@"INSERT INTO ParliamentAchievements(
AchievementCode, Title, Sector, AchievementType, Governorate, District, SubDistrict,
ResponsibleOrganization, ProblemStatement, OfficeAction, ResultSummary, PublicImpact,
BeneficiariesCount, EstimatedCost, ProgressPercent, Status, ReliabilityStatus, ReliabilityScore,
StartedAt, CompletedAt, CreatedBy)
VALUES($code,$title,$sector,$type,$gov,$district,$sub,$org,$problem,$action,$result,$impact,$beneficiaries,$cost,$progress,$status,$rstatus,$rscore,$start,$complete,$user)",
            new SqliteParameter("$code", NullIfEmpty(item.AchievementCode)),
            new SqliteParameter("$title", item.Title),
            new SqliteParameter("$sector", NullIfEmpty(item.Sector)),
            new SqliteParameter("$type", NullIfEmpty(item.AchievementType)),
            new SqliteParameter("$gov", NullIfEmpty(item.Governorate)),
            new SqliteParameter("$district", NullIfEmpty(item.District)),
            new SqliteParameter("$sub", NullIfEmpty(item.SubDistrict)),
            new SqliteParameter("$org", NullIfEmpty(item.ResponsibleOrganization)),
            new SqliteParameter("$problem", NullIfEmpty(item.ProblemStatement)),
            new SqliteParameter("$action", NullIfEmpty(item.OfficeAction)),
            new SqliteParameter("$result", NullIfEmpty(item.ResultSummary)),
            new SqliteParameter("$impact", NullIfEmpty(item.PublicImpact)),
            new SqliteParameter("$beneficiaries", item.BeneficiariesCount),
            new SqliteParameter("$cost", item.EstimatedCost == null ? DBNull.Value : item.EstimatedCost),
            new SqliteParameter("$progress", item.ProgressPercent),
            new SqliteParameter("$status", item.Status),
            new SqliteParameter("$rstatus", item.ReliabilityStatus),
            new SqliteParameter("$rscore", item.ReliabilityScore),
            new SqliteParameter("$start", item.StartedAt == null ? DBNull.Value : item.StartedAt.Value.ToString("yyyy-MM-dd")),
            new SqliteParameter("$complete", item.CompletedAt == null ? DBNull.Value : item.CompletedAt.Value.ToString("yyyy-MM-dd")),
            new SqliteParameter("$user", userId == null ? DBNull.Value : userId));

        return Convert.ToInt32(Db.Scalar("SELECT last_insert_rowid()") ?? 0);
    }

    public void AddEvidence(int achievementId, AchievementEvidence evidence)
    {
        Db.Execute(@"INSERT INTO AchievementEvidenceItems(AchievementId,EvidenceType,Title,ReferenceNumber,ReferenceDate,SourceOrganization,FilePath,Notes,Weight)
VALUES($aid,$type,$title,$ref,$date,$source,$path,$notes,$weight)",
            new SqliteParameter("$aid", achievementId),
            new SqliteParameter("$type", evidence.EvidenceType),
            new SqliteParameter("$title", evidence.Title),
            new SqliteParameter("$ref", NullIfEmpty(evidence.ReferenceNumber)),
            new SqliteParameter("$date", evidence.ReferenceDate == null ? DBNull.Value : evidence.ReferenceDate.Value.ToString("yyyy-MM-dd")),
            new SqliteParameter("$source", NullIfEmpty(evidence.SourceOrganization)),
            new SqliteParameter("$path", NullIfEmpty(evidence.FilePath)),
            new SqliteParameter("$notes", NullIfEmpty(evidence.Notes)),
            new SqliteParameter("$weight", evidence.Weight));
    }

    public void LinkEntity(int achievementId, string entityType, int entityId, string reason)
    {
        Db.Execute(@"INSERT INTO AchievementLinks(AchievementId,EntityType,EntityId,LinkReason)
VALUES($aid,$type,$id,$reason)",
            new SqliteParameter("$aid", achievementId),
            new SqliteParameter("$type", entityType),
            new SqliteParameter("$id", entityId),
            new SqliteParameter("$reason", NullIfEmpty(reason)));
    }

    public DataTable QueryAchievements()
    {
        return Db.Query(@"SELECT Id,
AchievementCode AS 'رقم المنجز',
Title AS 'عنوان المنجز',
Sector AS 'القطاع',
ResponsibleOrganization AS 'الجهة',
BeneficiariesCount AS 'المستفيدون',
ProgressPercent AS 'نسبة الإنجاز',
ReliabilityScore AS 'درجة التوثيق',
ReliabilityStatus AS 'حالة التوثيق',
Status AS 'الحالة',
CreatedAt AS 'تاريخ الإدخال'
FROM ParliamentAchievements ORDER BY Id DESC");
    }

    public DataTable QueryAchievementEvidence(int achievementId)
    {
        return Db.Query(@"SELECT EvidenceType AS 'نوع الدليل', Title AS 'العنوان', ReferenceNumber AS 'رقم المرجع', ReferenceDate AS 'تاريخ المرجع', SourceOrganization AS 'الجهة', FilePath AS 'مسار الملف', Weight AS 'الوزن'
FROM AchievementEvidenceItems WHERE AchievementId=$id ORDER BY Id DESC", new SqliteParameter("$id", achievementId));
    }

    private static object NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
    }
}
