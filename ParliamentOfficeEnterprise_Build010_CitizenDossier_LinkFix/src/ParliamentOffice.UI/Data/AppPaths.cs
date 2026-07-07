using System;
using System.IO;

namespace ParliamentOffice.UI.Data;

public static class AppPaths
{
    public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ParliamentOfficeEnterprise");
    public static string DatabaseFolder => Path.Combine(Root, "Database");
    public static string DatabasePath => Path.Combine(DatabaseFolder, "ParliamentOffice.db");
    public static string Attachments => Path.Combine(Root, "Attachments");
    public static string IncomingAttachments => Path.Combine(Attachments, "Incoming");
    public static string OutgoingAttachments => Path.Combine(Attachments, "Outgoing");
    public static string CitizenAttachments => Path.Combine(Attachments, "Citizens");
    public static string AchievementAttachments => Path.Combine(Attachments, "Achievements");
    public static string Backups => Path.Combine(Root, "Backups");
    public static string Reports => Path.Combine(Root, "Reports");
    public static string Logs => Path.Combine(Root, "Logs");
    public static string ConnectionString => $"Data Source={DatabasePath}";

    public static void EnsureFolders()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(DatabaseFolder);
        Directory.CreateDirectory(IncomingAttachments);
        Directory.CreateDirectory(OutgoingAttachments);
        Directory.CreateDirectory(CitizenAttachments);
        Directory.CreateDirectory(AchievementAttachments);
        Directory.CreateDirectory(Backups);
        Directory.CreateDirectory(Reports);
        Directory.CreateDirectory(Logs);
    }
}
