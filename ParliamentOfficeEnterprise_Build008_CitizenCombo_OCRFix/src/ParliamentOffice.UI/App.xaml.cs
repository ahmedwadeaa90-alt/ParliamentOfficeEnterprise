using System.IO;
using System.Windows;
using ParliamentOffice.UI.Data;
using ParliamentOffice.UI.Views;

namespace ParliamentOffice.UI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            DatabaseInitializer.Initialize();
            var login = new LoginWindow();
            login.Show();
        }
        catch (Exception ex)
        {
            try
            {
                AppPaths.EnsureFolders();
                File.WriteAllText(Path.Combine(AppPaths.Logs, $"StartupError_{DateTime.Now:yyyyMMdd_HHmmss}.txt"), ex.ToString());
            }
            catch { }
            MessageBox.Show(ex.Message, "خطأ تشغيل البرنامج", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}
