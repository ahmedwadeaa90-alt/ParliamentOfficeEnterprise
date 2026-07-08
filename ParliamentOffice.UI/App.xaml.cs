using System.Windows;
using ParliamentOffice.UI.Data;
using ParliamentOffice.UI.Views;

namespace ParliamentOffice.UI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DatabaseInitializer.Initialize();
        var login = new LoginWindow();
        login.Show();
    }
}
