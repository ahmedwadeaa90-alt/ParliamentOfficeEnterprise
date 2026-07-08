using System.Windows;
using System.Windows.Input;
using Microsoft.Data.Sqlite;
using ParliamentOffice.UI.Data;
using ParliamentOffice.UI.Services;

namespace ParliamentOffice.UI.Views;

public partial class LoginWindow : Window
{
    private readonly AuthService _auth = new();

    public LoginWindow()
    {
        InitializeComponent();
    }

    private void Login_Click(object sender, RoutedEventArgs e) => TryLogin();

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) TryLogin();
    }

    private void ResetAdmin_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Db.Execute(@"INSERT OR IGNORE INTO Users(Id, FullName, Username, PasswordHash, Role, IsActive)
VALUES(1, 'مدير النظام', 'admin', $hash, 'مدير', 1);", new SqliteParameter("$hash", PasswordService.Hash("1234")));
            Db.Execute("UPDATE Users SET PasswordHash=$hash, Role='مدير', IsActive=1 WHERE Username='admin'", new SqliteParameter("$hash", PasswordService.Hash("1234")));
            ErrorText.Foreground = System.Windows.Media.Brushes.DarkGreen;
            ErrorText.Text = "تم إصلاح حساب المدير. استخدم admin / 1234";
        }
        catch (Exception ex)
        {
            ErrorText.Foreground = System.Windows.Media.Brushes.DarkRed;
            ErrorText.Text = ex.Message;
        }
    }

    private void TryLogin()
    {
        ErrorText.Foreground = System.Windows.Media.Brushes.DarkRed;
        ErrorText.Text = string.Empty;
        if (_auth.Login(UsernameBox.Text, PasswordBox.Password, out var id, out var name, out var role))
        {
            AppSession.UserId = id;
            AppSession.FullName = name;
            AppSession.Role = role;
            var main = new MainWindow();
            Application.Current.MainWindow = main;
            main.Show();
            Close();
            return;
        }
        ErrorText.Text = "بيانات الدخول غير صحيحة. اضغط زر إصلاح حساب المدير ثم جرّب ثانية.";
    }
}
