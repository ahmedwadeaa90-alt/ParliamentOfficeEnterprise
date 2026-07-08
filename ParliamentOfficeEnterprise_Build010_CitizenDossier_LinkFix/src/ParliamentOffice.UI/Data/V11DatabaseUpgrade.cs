using Microsoft.Data.Sqlite;

namespace ParliamentOffice.UI.Data;

public static class V11DatabaseUpgrade
{
    public static void Apply()
    {
        using var connection = new SqliteConnection(AppPaths.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS ParliamentAchievements (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AchievementCode TEXT UNIQUE,
    Title TEXT NOT NULL,
    Sector TEXT,
    AchievementType TEXT,
    Governorate TEXT,
    District TEXT,
    SubDistrict TEXT,
    ResponsibleOrganization TEXT,
    ProblemStatement TEXT,
    OfficeAction TEXT,
    ResultSummary TEXT,
    PublicImpact TEXT,
    BeneficiariesCount INTEGER NOT NULL DEFAULT 0,
    EstimatedCost REAL,
    ProgressPercent INTEGER NOT NULL DEFAULT 0,
    Status TEXT NOT NULL DEFAULT 'قيد المتابعة',
    ReliabilityStatus TEXT NOT NULL DEFAULT 'غير مدقق',
    ReliabilityScore INTEGER NOT NULL DEFAULT 0,
    StartedAt TEXT,
    CompletedAt TEXT,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT,
    FOREIGN KEY(CreatedBy) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS AchievementEvidenceItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AchievementId INTEGER NOT NULL,
    EvidenceType TEXT NOT NULL,
    Title TEXT NOT NULL,
    ReferenceNumber TEXT,
    ReferenceDate TEXT,
    SourceOrganization TEXT,
    FilePath TEXT,
    Notes TEXT,
    Weight INTEGER NOT NULL DEFAULT 10,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(AchievementId) REFERENCES ParliamentAchievements(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS AchievementLinks (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AchievementId INTEGER NOT NULL,
    EntityType TEXT NOT NULL,
    EntityId INTEGER NOT NULL,
    LinkReason TEXT,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(AchievementId) REFERENCES ParliamentAchievements(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS AchievementCrossCheckLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AchievementId INTEGER NOT NULL,
    LinkedOutgoingCount INTEGER NOT NULL DEFAULT 0,
    LinkedIncomingCount INTEGER NOT NULL DEFAULT 0,
    EvidenceCount INTEGER NOT NULL DEFAULT 0,
    AttachmentCount INTEGER NOT NULL DEFAULT 0,
    ReliabilityScore INTEGER NOT NULL DEFAULT 0,
    ReliabilityStatus TEXT,
    MissingItems TEXT,
    Warnings TEXT,
    CanBeIncludedInFinalReport INTEGER NOT NULL DEFAULT 0,
    CheckedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(AchievementId) REFERENCES ParliamentAchievements(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_ParliamentAchievements_Title ON ParliamentAchievements(Title);
CREATE INDEX IF NOT EXISTS IX_ParliamentAchievements_Sector ON ParliamentAchievements(Sector);
CREATE INDEX IF NOT EXISTS IX_ParliamentAchievements_Status ON ParliamentAchievements(Status);
CREATE INDEX IF NOT EXISTS IX_AchievementEvidence_Achievement ON AchievementEvidenceItems(AchievementId);
CREATE INDEX IF NOT EXISTS IX_AchievementLinks_Achievement ON AchievementLinks(AchievementId);
CREATE INDEX IF NOT EXISTS IX_AchievementLinks_Entity ON AchievementLinks(EntityType, EntityId);
";
        command.ExecuteNonQuery();
    }
}
