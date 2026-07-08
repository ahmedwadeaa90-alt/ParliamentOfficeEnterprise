using System.IO;
using ParliamentOffice.UI.Data;

namespace ParliamentOffice.UI.Services;

public static class BackupService
{
    public static string CreateBackup()
    {
        AppPaths.EnsureFolders();
        var name = $"ParliamentOffice_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        var target = Path.Combine(AppPaths.Backups, name);
        File.Copy(AppPaths.DatabasePath, target, true);
        return target;
    }

    public static void RestoreBackup(string backupFile)
    {
        AppPaths.EnsureFolders();
        if (!File.Exists(backupFile)) throw new FileNotFoundException("ملف النسخة الاحتياطية غير موجود", backupFile);
        var beforeRestore = Path.Combine(AppPaths.Backups, $"BeforeRestore_{DateTime.Now:yyyyMMdd_HHmmss}.db");
        if (File.Exists(AppPaths.DatabasePath)) File.Copy(AppPaths.DatabasePath, beforeRestore, true);
        File.Copy(backupFile, AppPaths.DatabasePath, true);
    }
}
