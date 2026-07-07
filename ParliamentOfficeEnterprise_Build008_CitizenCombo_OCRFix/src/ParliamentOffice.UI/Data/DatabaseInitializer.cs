using Microsoft.Data.Sqlite;
using ParliamentOffice.UI.Services;
using System.IO;

namespace ParliamentOffice.UI.Data;

public static class DatabaseInitializer
{
    private const int CurrentDbVersion = 6;

    public static void Initialize()
    {
        AppPaths.EnsureFolders();
        BackupBeforeUpgradeIfNeeded();

        using var connection = new SqliteConnection(AppPaths.ConnectionString);
        connection.Open();

        Execute(connection, "PRAGMA foreign_keys = OFF;");
        CreateBaseTables(connection);
        ApplyMigrations(connection);
        CreateIndexes(connection);
        Seed(connection);
        Execute(connection, "PRAGMA foreign_keys = ON;");
    }

    private static void BackupBeforeUpgradeIfNeeded()
    {
        try
        {
            if (!File.Exists(AppPaths.DatabasePath)) return;
            var target = Path.Combine(AppPaths.Backups, $"BeforeUpgrade_Build006_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            File.Copy(AppPaths.DatabasePath, target, true);
        }
        catch
        {
            // لا نوقف البرنامج إذا فشلت النسخة الاحتياطية، لكن تتم محاولة الإنشاء دائماً.
        }
    }

    private static void CreateBaseTables(SqliteConnection connection)
    {
        Execute(connection, @"
CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL,
    Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    Role TEXT NOT NULL DEFAULT 'مدير',
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Organizations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    Type TEXT,
    ParentName TEXT,
    Phone TEXT,
    Email TEXT,
    Notes TEXT,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Citizens (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL,
    Phone TEXT,
    Address TEXT,
    RequestSubject TEXT NOT NULL DEFAULT 'أضبارة مواطن',
    Status TEXT NOT NULL DEFAULT 'جديد',
    FollowUpRequired INTEGER NOT NULL DEFAULT 0,
    Notes TEXT,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT,
    FOREIGN KEY(CreatedBy) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS IncomingLetters (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IncomingNumber TEXT NOT NULL,
    IncomingDate TEXT NOT NULL,
    SourceOrganizationId INTEGER,
    CitizenId INTEGER,
    Subject TEXT NOT NULL,
    LetterType TEXT DEFAULT 'كتاب',
    Priority TEXT DEFAULT 'عادي',
    Status TEXT NOT NULL DEFAULT 'جديد',
    FollowUpRequired INTEGER NOT NULL DEFAULT 0,
    Notes TEXT,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT,
    FOREIGN KEY(SourceOrganizationId) REFERENCES Organizations(Id),
    FOREIGN KEY(CitizenId) REFERENCES Citizens(Id),
    FOREIGN KEY(CreatedBy) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS OutgoingLetters (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OutgoingNumber TEXT NOT NULL,
    OutgoingDate TEXT NOT NULL,
    TargetOrganizationId INTEGER,
    CitizenId INTEGER,
    Subject TEXT NOT NULL,
    RelatedIncomingId INTEGER,
    Priority TEXT DEFAULT 'عادي',
    Status TEXT NOT NULL DEFAULT 'جديد',
    FollowUpRequired INTEGER NOT NULL DEFAULT 0,
    Notes TEXT,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT,
    FOREIGN KEY(TargetOrganizationId) REFERENCES Organizations(Id),
    FOREIGN KEY(CitizenId) REFERENCES Citizens(Id),
    FOREIGN KEY(RelatedIncomingId) REFERENCES IncomingLetters(Id),
    FOREIGN KEY(CreatedBy) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS Attachments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EntityType TEXT NOT NULL,
    EntityId INTEGER NOT NULL,
    FileName TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    FileExtension TEXT,
    FileSize INTEGER,
    UploadedBy INTEGER,
    UploadedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Notes TEXT,
    FOREIGN KEY(UploadedBy) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS Followups (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EntityType TEXT NOT NULL,
    EntityId INTEGER NOT NULL,
    FollowupDate TEXT NOT NULL,
    ActionTaken TEXT NOT NULL,
    NextActionDate TEXT,
    Status TEXT NOT NULL DEFAULT 'مفتوح',
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(CreatedBy) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS ActivityLog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER,
    ActionType TEXT NOT NULL,
    EntityType TEXT,
    EntityId INTEGER,
    Description TEXT,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(UserId) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS Settings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SettingKey TEXT NOT NULL UNIQUE,
    SettingValue TEXT,
    Notes TEXT
);
");
    }

    private static void ApplyMigrations(SqliteConnection connection)
    {
        // Build006: لا يفترض أن قاعدة البيانات نظيفة. يضيف كل عمود ناقص قبل إنشاء الفهارس.
        EnsureColumn(connection, "Citizens", "Phone", "TEXT");
        EnsureColumn(connection, "Citizens", "Address", "TEXT");
        EnsureColumn(connection, "Citizens", "RequestSubject", "TEXT NOT NULL DEFAULT 'أضبارة مواطن'");
        EnsureColumn(connection, "Citizens", "Status", "TEXT NOT NULL DEFAULT 'جديد'");
        EnsureColumn(connection, "Citizens", "FollowUpRequired", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Citizens", "Notes", "TEXT");
        EnsureColumn(connection, "Citizens", "CreatedBy", "INTEGER");
        EnsureColumn(connection, "Citizens", "CreatedAt", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");
        EnsureColumn(connection, "Citizens", "UpdatedAt", "TEXT");

        EnsureColumn(connection, "IncomingLetters", "SourceOrganizationId", "INTEGER");
        EnsureColumn(connection, "IncomingLetters", "CitizenId", "INTEGER");
        EnsureColumn(connection, "IncomingLetters", "LetterType", "TEXT DEFAULT 'كتاب'");
        EnsureColumn(connection, "IncomingLetters", "Priority", "TEXT DEFAULT 'عادي'");
        EnsureColumn(connection, "IncomingLetters", "Status", "TEXT NOT NULL DEFAULT 'جديد'");
        EnsureColumn(connection, "IncomingLetters", "FollowUpRequired", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "IncomingLetters", "Notes", "TEXT");
        EnsureColumn(connection, "IncomingLetters", "CreatedBy", "INTEGER");
        EnsureColumn(connection, "IncomingLetters", "CreatedAt", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");
        EnsureColumn(connection, "IncomingLetters", "UpdatedAt", "TEXT");

        EnsureColumn(connection, "OutgoingLetters", "TargetOrganizationId", "INTEGER");
        EnsureColumn(connection, "OutgoingLetters", "CitizenId", "INTEGER");
        EnsureColumn(connection, "OutgoingLetters", "RelatedIncomingId", "INTEGER");
        EnsureColumn(connection, "OutgoingLetters", "Priority", "TEXT DEFAULT 'عادي'");
        EnsureColumn(connection, "OutgoingLetters", "Status", "TEXT NOT NULL DEFAULT 'جديد'");
        EnsureColumn(connection, "OutgoingLetters", "FollowUpRequired", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "OutgoingLetters", "Notes", "TEXT");
        EnsureColumn(connection, "OutgoingLetters", "CreatedBy", "INTEGER");
        EnsureColumn(connection, "OutgoingLetters", "CreatedAt", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");
        EnsureColumn(connection, "OutgoingLetters", "UpdatedAt", "TEXT");

        EnsureColumn(connection, "Attachments", "Notes", "TEXT");

        SetSetting(connection, "DatabaseVersion", CurrentDbVersion.ToString(), "إصدار قاعدة البيانات");
    }

    private static void CreateIndexes(SqliteConnection connection)
    {
        Execute(connection, @"
CREATE INDEX IF NOT EXISTS IX_Incoming_Number ON IncomingLetters(IncomingNumber);
CREATE INDEX IF NOT EXISTS IX_Outgoing_Number ON OutgoingLetters(OutgoingNumber);
CREATE INDEX IF NOT EXISTS IX_Incoming_Subject ON IncomingLetters(Subject);
CREATE INDEX IF NOT EXISTS IX_Outgoing_Subject ON OutgoingLetters(Subject);
CREATE INDEX IF NOT EXISTS IX_Citizens_Name ON Citizens(FullName);
CREATE INDEX IF NOT EXISTS IX_Citizens_Subject ON Citizens(RequestSubject);
CREATE INDEX IF NOT EXISTS IX_Incoming_Citizen ON IncomingLetters(CitizenId);
CREATE INDEX IF NOT EXISTS IX_Outgoing_Citizen ON OutgoingLetters(CitizenId);
CREATE INDEX IF NOT EXISTS IX_Attachments_Entity ON Attachments(EntityType, EntityId);
");
    }

    private static void EnsureColumn(SqliteConnection connection, string table, string column, string type)
    {
        using var check = connection.CreateCommand();
        check.CommandText = $"PRAGMA table_info({table})";
        using var reader = check.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader["name"].ToString(), column, StringComparison.OrdinalIgnoreCase)) return;
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {type}";
        alter.ExecuteNonQuery();
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static void SetSetting(SqliteConnection connection, string key, string value, string notes)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Settings(SettingKey, SettingValue, Notes) VALUES($k,$v,$n)
ON CONFLICT(SettingKey) DO UPDATE SET SettingValue=excluded.SettingValue, Notes=excluded.Notes;";
        cmd.Parameters.AddWithValue("$k", key);
        cmd.Parameters.AddWithValue("$v", value);
        cmd.Parameters.AddWithValue("$n", notes);
        cmd.ExecuteNonQuery();
    }

    private static void Seed(SqliteConnection connection)
    {
        using var user = connection.CreateCommand();
        user.CommandText = @"INSERT OR IGNORE INTO Users(Id, FullName, Username, PasswordHash, Role, IsActive)
VALUES(1, 'مدير النظام', 'admin', $hash, 'مدير', 1);";
        user.Parameters.AddWithValue("$hash", PasswordService.Hash("1234"));
        user.ExecuteNonQuery();

        // إصلاح حساب المدير في قواعد البيانات القديمة.
        user.Parameters.Clear();
        user.CommandText = @"UPDATE Users SET PasswordHash=$hash, IsActive=1, Role='مدير' WHERE Username='admin';";
        user.Parameters.AddWithValue("$hash", PasswordService.Hash("1234"));
        user.ExecuteNonQuery();

        SetSetting(connection, "OfficeName", "مكتب النائب", "اسم المكتب");
        SetSetting(connection, "MPName", "النائب رياض عباس التميمي", "اسم النائب");
        EnsureSetting(connection, "LastOutgoingNumber", "0", "آخر رقم صادر");
        EnsureSetting(connection, "LastIncomingNumber", "0", "آخر رقم وارد");
        EnsureSetting(connection, "AttachmentRootPath", "", "مسار المرفقات");
        EnsureSetting(connection, "BackupPath", "", "مسار النسخ الاحتياطي");

        SeedOrganizations(connection);
    }

    private static void EnsureSetting(SqliteConnection connection, string key, string value, string notes)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO Settings(SettingKey, SettingValue, Notes) VALUES($k,$v,$n);";
        cmd.Parameters.AddWithValue("$k", key);
        cmd.Parameters.AddWithValue("$v", value);
        cmd.Parameters.AddWithValue("$n", notes);
        cmd.ExecuteNonQuery();
    }

    private static void SeedOrganizations(SqliteConnection connection)
    {
        string[] rows =
        {
            "رئاسة الجمهورية|رئاسة", "مجلس النواب العراقي|سلطة تشريعية", "مجلس الوزراء|جهة حكومية", "الأمانة العامة لمجلس الوزراء|جهة حكومية", "مكتب رئيس مجلس الوزراء|جهة حكومية",
            "وزارة المالية|وزارة", "وزارة التخطيط|وزارة", "وزارة الداخلية|وزارة", "وزارة الدفاع|وزارة", "وزارة الخارجية|وزارة", "وزارة العدل|وزارة", "وزارة النفط|وزارة", "وزارة الكهرباء|وزارة", "وزارة الصحة|وزارة", "وزارة التربية|وزارة", "وزارة التعليم العالي والبحث العلمي|وزارة", "وزارة الإعمار والإسكان والبلديات العامة|وزارة", "وزارة النقل|وزارة", "وزارة الاتصالات|وزارة", "وزارة الموارد المائية|وزارة", "وزارة الزراعة|وزارة", "وزارة الصناعة والمعادن|وزارة", "وزارة التجارة|وزارة", "وزارة العمل والشؤون الاجتماعية|وزارة", "وزارة الهجرة والمهجرين|وزارة", "وزارة الثقافة والسياحة والآثار|وزارة", "وزارة الشباب والرياضة|وزارة", "وزارة البيئة|وزارة",
            "جهاز الأمن الوطني العراقي|جهاز أمني", "مستشارية الأمن القومي|جهاز أمني", "هيئة الحشد الشعبي|جهاز أمني", "جهاز المخابرات الوطني العراقي|جهاز أمني", "قيادة العمليات المشتركة|جهاز أمني", "قيادة عمليات بغداد|جهاز أمني", "وكالة الاستخبارات والتحقيقات الاتحادية|جهاز أمني", "قيادة قوات الشرطة الاتحادية|جهاز أمني", "جهاز مكافحة الإرهاب|جهاز أمني",
            "مديرية المرور العامة|دائرة", "مديرية الأحوال المدنية والجوازات والإقامة|دائرة", "مديرية الدفاع المدني العامة|دائرة", "هيئة النزاهة الاتحادية|هيئة", "ديوان الرقابة المالية الاتحادي|هيئة", "مفوضية الانتخابات|هيئة", "هيئة الإعلام والاتصالات|هيئة", "هيئة المنافذ الحدودية|هيئة", "هيئة التقاعد الوطنية|هيئة", "هيئة الضرائب العامة|دائرة", "الهيئة العامة للكمارك|دائرة", "البنك المركزي العراقي|مؤسسة", "مؤسسة الشهداء|مؤسسة", "مؤسسة السجناء السياسيين|مؤسسة",
            "محافظة بغداد|محافظة", "محافظة ديالى|محافظة", "محافظة بابل|محافظة", "محافظة كربلاء المقدسة|محافظة", "محافظة النجف الأشرف|محافظة", "محافظة واسط|محافظة", "محافظة ميسان|محافظة", "محافظة ذي قار|محافظة", "محافظة المثنى|محافظة", "محافظة البصرة|محافظة", "محافظة الأنبار|محافظة", "محافظة صلاح الدين|محافظة", "محافظة نينوى|محافظة", "محافظة كركوك|محافظة", "محافظة أربيل|محافظة", "محافظة السليمانية|محافظة", "محافظة دهوك|محافظة",
            "مجلس محافظة بغداد|مجلس محافظة", "مجلس محافظة ديالى|مجلس محافظة", "مجلس محافظة بابل|مجلس محافظة", "مجلس محافظة كربلاء المقدسة|مجلس محافظة", "مجلس محافظة النجف الأشرف|مجلس محافظة", "مجلس محافظة واسط|مجلس محافظة", "مجلس محافظة ميسان|مجلس محافظة", "مجلس محافظة ذي قار|مجلس محافظة", "مجلس محافظة المثنى|مجلس محافظة", "مجلس محافظة البصرة|مجلس محافظة", "مجلس محافظة الأنبار|مجلس محافظة", "مجلس محافظة صلاح الدين|مجلس محافظة", "مجلس محافظة نينوى|مجلس محافظة", "مجلس محافظة كركوك|مجلس محافظة",
            "أمانة بغداد|دائرة", "محافظة ديالى / ديوان المحافظة|محافظة", "دائرة صحة ديالى|دائرة", "مديرية تربية ديالى|دائرة", "مديرية بلدية بعقوبة|دائرة", "مديرية ماء ديالى|دائرة", "مديرية مجاري ديالى|دائرة", "مديرية طرق وجسور ديالى|دائرة", "مديرية زراعة ديالى|دائرة", "مديرية كهرباء ديالى|دائرة", "جامعة ديالى|جامعة", "جامعة بغداد|جامعة", "جامعة النهرين|جامعة", "الجامعة العراقية|جامعة", "جامعة بابل|جامعة"
        };

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO Organizations(Name, Type) VALUES($n,$t);";
        var n = cmd.Parameters.Add("$n", SqliteType.Text);
        var t = cmd.Parameters.Add("$t", SqliteType.Text);
        foreach (var row in rows)
        {
            var parts = row.Split('|');
            n.Value = parts[0];
            t.Value = parts.Length > 1 ? parts[1] : "أخرى";
            cmd.ExecuteNonQuery();
        }
    }
}
