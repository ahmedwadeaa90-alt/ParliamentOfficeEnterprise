using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using ParliamentOffice.UI.Data;
using ParliamentOffice.UI.Services;

namespace ParliamentOffice.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        UserText.Text = $"المستخدم: {AppSession.FullName} - الصلاحية: {AppSession.Role}";
        IncomingDatePicker.SelectedDate = DateTime.Today;
        OutgoingDatePicker.SelectedDate = DateTime.Today;
        FollowNextDatePicker.SelectedDate = DateTime.Today.AddDays(7);
        ActivityDatePicker.SelectedDate = DateTime.Today;
        AchievementReportFromDate.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        AchievementReportToDate.SelectedDate = DateTime.Today;
        LoadAll();
        GenerateOutgoingNumber();
        GenerateIncomingNumber();
    }

    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl) LoadAllSafe();
    }

    private void LoadAllSafe() { try { LoadAll(); } catch { } }

    private void LoadAll()
    {
        LoadOrganizations(); LoadCitizens(); LoadCitizenCombos(); LoadIncomingCombos(); LoadOutgoingCombos(); LoadOutgoing(); LoadIncoming(); LoadFollowups(); LoadAchievements(); LoadUsers(); RefreshDashboard();
    }

    private static string ComboText(ComboBox combo) => (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? combo.Text ?? string.Empty;
    private static int? ToInt(object? value) => value == null || value == DBNull.Value ? null : int.TryParse(value.ToString(), out var n) ? n : null;

    private static int NextNumberFromDb(string table, string column, string settingKey)
    {
        var maxDb = Convert.ToInt32(Db.Scalar($"SELECT COALESCE(MAX(CAST({column} AS INTEGER)),0) FROM {table} WHERE {column} GLOB '[0-9]*'") ?? 0);
        var setting = Convert.ToInt32(Db.Scalar("SELECT COALESCE(MAX(CAST(SettingValue AS INTEGER)),0) FROM Settings WHERE SettingKey=$k", new SqliteParameter("$k", settingKey)) ?? 0);
        return Math.Max(maxDb, setting) + 1;
    }

    private void GenerateOutgoingNumber() => OutgoingNumberBox.Text = NextNumberFromDb("OutgoingLetters", "OutgoingNumber", "LastOutgoingNumber").ToString();
    private void GenerateIncomingNumber() => IncomingNumberBox.Text = NextNumberFromDb("IncomingLetters", "IncomingNumber", "LastIncomingNumber").ToString();
    private void GenerateOutgoingNumber_Click(object sender, RoutedEventArgs e) => GenerateOutgoingNumber();
    private void GenerateIncomingNumber_Click(object sender, RoutedEventArgs e) => GenerateIncomingNumber();

    private void LoadOrganizations()
    {
        OrganizationsGrid.ItemsSource = Db.Query("SELECT Id, Name AS 'اسم الجهة', Type AS 'النوع', CreatedAt AS 'تاريخ الإضافة' FROM Organizations ORDER BY Name").DefaultView;
        var comboTable = Db.Query("SELECT Id, Name FROM Organizations ORDER BY Name");
        OutgoingOrgCombo.ItemsSource = comboTable.DefaultView;
        IncomingOrgCombo.ItemsSource = comboTable.DefaultView;
        ActivityOrgCombo.ItemsSource = comboTable.DefaultView;
    }
    private void LoadCitizenCombos()
    {
        var citizens = Db.Query("SELECT Id, FullName FROM Citizens ORDER BY FullName");
        OutgoingCitizenCombo.ItemsSource = citizens.DefaultView;
        IncomingCitizenCombo.ItemsSource = citizens.DefaultView;
    }

    private void LoadIncomingCombos()
    {
        var table = Db.Query("SELECT Id, IncomingNumber || ' - ' || Subject AS Title FROM IncomingLetters ORDER BY Id DESC");
        OutgoingRelatedIncomingCombo.ItemsSource = table.DefaultView; LinkIncomingCombo.ItemsSource = table.DefaultView;
    }
    private void LoadOutgoingCombos()
    {
        LinkOutgoingCombo.ItemsSource = Db.Query("SELECT Id, OutgoingNumber || ' - ' || Subject AS Title FROM OutgoingLetters ORDER BY Id DESC").DefaultView;
    }
    private void LoadOutgoing()
    {
        OutgoingGrid.ItemsSource = Db.Query(@"SELECT o.Id, o.CitizenId, o.OutgoingNumber AS 'رقم الصادر', o.OutgoingDate AS 'تاريخ الصادر', org.Name AS 'الجهة', c.FullName AS 'المواطن', o.Subject AS 'الموضوع',
COALESCE(i.IncomingNumber || ' - ' || i.Subject, '') AS 'مرتبط بوارد', o.Priority AS 'الأولوية', o.Status AS 'الحالة', CASE o.FollowUpRequired WHEN 1 THEN 'نعم' ELSE 'لا' END AS 'متابعة', o.CreatedAt AS 'تاريخ الإدخال'
FROM OutgoingLetters o LEFT JOIN Organizations org ON org.Id=o.TargetOrganizationId LEFT JOIN Citizens c ON c.Id=o.CitizenId LEFT JOIN IncomingLetters i ON i.Id=o.RelatedIncomingId ORDER BY o.Id DESC").DefaultView;
    }
    private void LoadIncoming()
    {
        IncomingGrid.ItemsSource = Db.Query(@"SELECT i.Id, i.CitizenId, i.IncomingNumber AS 'رقم الوارد', i.IncomingDate AS 'تاريخ الوارد', org.Name AS 'الجهة', c.FullName AS 'المواطن', i.Subject AS 'الموضوع', i.Priority AS 'الأولوية', i.Status AS 'الحالة', CASE i.FollowUpRequired WHEN 1 THEN 'نعم' ELSE 'لا' END AS 'متابعة', i.CreatedAt AS 'تاريخ الإدخال'
FROM IncomingLetters i LEFT JOIN Organizations org ON org.Id=i.SourceOrganizationId LEFT JOIN Citizens c ON c.Id=i.CitizenId ORDER BY i.Id DESC").DefaultView;
    }
    private void LoadCitizens()
    {
        CitizensGrid.ItemsSource = Db.Query(@"SELECT c.Id, c.FullName AS 'اسم المواطن', c.Phone AS 'الهاتف', c.Address AS 'العنوان', c.RequestSubject AS 'موضوع الطلب',
       c.Status AS 'الحالة', CASE c.FollowUpRequired WHEN 1 THEN 'نعم' ELSE 'لا' END AS 'متابعة',
       (SELECT COUNT(*) FROM OutgoingLetters o WHERE o.CitizenId=c.Id) AS 'عدد الصادر',
       (SELECT COUNT(*) FROM IncomingLetters i WHERE i.CitizenId=c.Id) AS 'عدد الوارد',
       (SELECT COUNT(*) FROM Attachments a WHERE (a.EntityType='Citizen' AND a.EntityId=c.Id)
          OR (a.EntityType='Outgoing' AND a.EntityId IN (SELECT Id FROM OutgoingLetters WHERE CitizenId=c.Id))
          OR (a.EntityType='Incoming' AND a.EntityId IN (SELECT Id FROM IncomingLetters WHERE CitizenId=c.Id))) AS 'عدد المرفقات',
       c.CreatedAt AS 'تاريخ الإدخال'
FROM Citizens c ORDER BY c.Id DESC").DefaultView;
    }
    private void LoadCitizenDossier(int citizenId)
    {
        CitizenOutgoingGrid.ItemsSource = Db.Query(@"SELECT o.Id, o.OutgoingNumber AS 'رقم الصادر', o.OutgoingDate AS 'التاريخ', org.Name AS 'الجهة', o.Subject AS 'الموضوع', o.Status AS 'الحالة', CASE o.FollowUpRequired WHEN 1 THEN 'نعم' ELSE 'لا' END AS 'متابعة'
FROM OutgoingLetters o LEFT JOIN Organizations org ON org.Id=o.TargetOrganizationId WHERE o.CitizenId=$cid ORDER BY o.Id DESC", new SqliteParameter("$cid", citizenId)).DefaultView;
        CitizenIncomingGrid.ItemsSource = Db.Query(@"SELECT i.Id, i.IncomingNumber AS 'رقم الوارد', i.IncomingDate AS 'التاريخ', org.Name AS 'الجهة', i.Subject AS 'الموضوع', i.Status AS 'الحالة', CASE i.FollowUpRequired WHEN 1 THEN 'نعم' ELSE 'لا' END AS 'متابعة'
FROM IncomingLetters i LEFT JOIN Organizations org ON org.Id=i.SourceOrganizationId WHERE i.CitizenId=$cid ORDER BY i.Id DESC", new SqliteParameter("$cid", citizenId)).DefaultView;
        CitizenAttachmentsGrid.ItemsSource = Db.Query(@"SELECT a.Id, CASE a.EntityType WHEN 'Outgoing' THEN 'صادر' WHEN 'Incoming' THEN 'وارد' ELSE 'مواطن' END AS 'النوع', a.EntityId AS 'رقم السجل', a.FileName AS 'اسم الملف', a.FilePath AS 'المسار', a.UploadedAt AS 'تاريخ الإرفاق'
FROM Attachments a
WHERE (a.EntityType='Citizen' AND a.EntityId=$cid)
   OR (a.EntityType='Outgoing' AND a.EntityId IN (SELECT Id FROM OutgoingLetters WHERE CitizenId=$cid))
   OR (a.EntityType='Incoming' AND a.EntityId IN (SELECT Id FROM IncomingLetters WHERE CitizenId=$cid))
ORDER BY a.Id DESC", new SqliteParameter("$cid", citizenId)).DefaultView;
    }

    private void ClearCitizenDossier()
    {
        CitizenOutgoingGrid.ItemsSource = null;
        CitizenIncomingGrid.ItemsSource = null;
        CitizenAttachmentsGrid.ItemsSource = null;
    }

    private void LoadFollowups()
    {
        FollowupsGrid.ItemsSource = Db.Query(@"SELECT Id, CASE EntityType WHEN 'Outgoing' THEN 'صادر' WHEN 'Incoming' THEN 'وارد' ELSE 'مواطن' END AS 'النوع', EntityId AS 'رقم السجل Id', FollowupDate AS 'تاريخ المتابعة', ActionTaken AS 'الإجراء', NextActionDate AS 'الإجراء القادم', Status AS 'الحالة', CreatedAt AS 'تاريخ الإدخال' FROM Followups ORDER BY Id DESC").DefaultView;
    }
    private void LoadUsers()
    {
        UsersGrid.ItemsSource = Db.Query("SELECT Id, FullName AS 'الاسم', Username AS 'اسم الدخول', Role AS 'الصلاحية', CASE IsActive WHEN 1 THEN 'فعال' ELSE 'معطل' END AS 'الحالة', CreatedAt AS 'تاريخ الإضافة' FROM Users ORDER BY Id").DefaultView;
    }
    private void RefreshDashboard()
    {
        OutgoingCount.Text = Convert.ToString(Db.Scalar("SELECT COUNT(*) FROM OutgoingLetters")) ?? "0";
        IncomingCount.Text = Convert.ToString(Db.Scalar("SELECT COUNT(*) FROM IncomingLetters")) ?? "0";
        CitizensCount.Text = Convert.ToString(Db.Scalar("SELECT COUNT(*) FROM Citizens")) ?? "0";
        OrgsCount.Text = Convert.ToString(Db.Scalar("SELECT COUNT(*) FROM Organizations")) ?? "0";
        FollowupCount.Text = Convert.ToString(Db.Scalar("SELECT (SELECT COUNT(*) FROM OutgoingLetters WHERE FollowUpRequired=1 AND Status!='مغلق') + (SELECT COUNT(*) FROM IncomingLetters WHERE FollowUpRequired=1 AND Status!='مغلق') + (SELECT COUNT(*) FROM Citizens WHERE FollowUpRequired=1 AND Status!='مغلق')")) ?? "0";
        LastOutgoingGrid.ItemsSource = Db.Query("SELECT OutgoingNumber AS 'الرقم', OutgoingDate AS 'التاريخ', Subject AS 'الموضوع', Status AS 'الحالة' FROM OutgoingLetters ORDER BY Id DESC LIMIT 8").DefaultView;
        LastIncomingGrid.ItemsSource = Db.Query("SELECT IncomingNumber AS 'الرقم', IncomingDate AS 'التاريخ', Subject AS 'الموضوع', Status AS 'الحالة' FROM IncomingLetters ORDER BY Id DESC LIMIT 8").DefaultView;
    }


    private void Grid_AutoGeneratedColumns(object? sender, EventArgs e)
    {
        if (sender is not DataGrid grid) return;
        foreach (var col in grid.Columns)
        {
            var h = col.Header?.ToString() ?? string.Empty;
            if (h == "Id" || h == "CitizenId" || h.EndsWith("Id")) col.Visibility = Visibility.Collapsed;
        }
    }

    private void LoadAchievements()
    {
        AchievementsGrid.ItemsSource = Db.Query(@"SELECT a.Id, a.ActivityCode AS 'رقم النشاط', a.ActivityDate AS 'التاريخ', a.Title AS 'العنوان', a.Category AS 'النوع', a.Governorate AS 'المحافظة', a.District AS 'القضاء', org.Name AS 'الجهة', a.Description AS 'الوصف', a.Keywords AS 'الوسوم', a.CreatedAt AS 'تاريخ الإدخال'
FROM Achievements a LEFT JOIN Organizations org ON org.Id=a.OrganizationId ORDER BY a.ActivityDate DESC, a.Id DESC").DefaultView;
    }

    private void AddOrg_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(OrgNameBox.Text)) { MessageBox.Show("اكتب اسم الجهة"); return; }
        Db.Execute("INSERT OR IGNORE INTO Organizations(Name, Type) VALUES($n,$t)", new SqliteParameter("$n", OrgNameBox.Text.Trim()), new SqliteParameter("$t", ComboText(OrgTypeBox)));
        OrgNameBox.Clear(); LoadOrganizations(); RefreshDashboard();
    }
    private int? EnsureCitizenFromCombo(ComboBox combo)
    {
        var typedName = combo.Text?.Trim() ?? string.Empty;
        if (combo.SelectedItem is DataRowView selectedRow)
        {
            var selectedName = selectedRow.Row.Table.Columns.Contains("FullName") ? selectedRow["FullName"]?.ToString()?.Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(selectedName) && string.Equals(selectedName, typedName, StringComparison.OrdinalIgnoreCase)
                && combo.SelectedValue != null && int.TryParse(combo.SelectedValue.ToString(), out var selectedId)) return selectedId;
        }
        if (string.IsNullOrWhiteSpace(typedName)) return null;
        var existing = ToInt(Db.Scalar("SELECT Id FROM Citizens WHERE FullName=$n ORDER BY Id DESC LIMIT 1", new SqliteParameter("$n", typedName)));
        if (existing != null) return existing;
        Db.Execute("INSERT INTO Citizens(FullName, RequestSubject, Status, FollowUpRequired, CreatedBy) VALUES($n, 'أضبارة مواطن', 'جديد', 0, $u)", new SqliteParameter("$n", typedName), new SqliteParameter("$u", AppSession.UserId));
        var id = ToInt(Db.Scalar("SELECT last_insert_rowid()"));
        LoadCitizens(); LoadCitizenCombos();
        return id;
    }

    private int? EnsureOrganizationFromCombo(ComboBox combo)
    {
        if (combo.SelectedValue != null && int.TryParse(combo.SelectedValue.ToString(), out var selectedId)) return selectedId;
        var name = combo.Text?.Trim(); if (string.IsNullOrWhiteSpace(name)) return null;
        Db.Execute("INSERT OR IGNORE INTO Organizations(Name, Type) VALUES($n,'أخرى')", new SqliteParameter("$n", name));
        var id = Db.Scalar("SELECT Id FROM Organizations WHERE Name=$n", new SqliteParameter("$n", name)); LoadOrganizations(); return ToInt(id);
    }

    private void AddOutgoing_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(OutgoingNumberBox.Text) || string.IsNullOrWhiteSpace(OutgoingSubjectBox.Text)) { MessageBox.Show("رقم الصادر والموضوع مطلوبان"); return; }
            var orgId = EnsureOrganizationFromCombo(OutgoingOrgCombo); var citizenId = EnsureCitizenFromCombo(OutgoingCitizenCombo); var relatedIncomingId = ToInt(OutgoingRelatedIncomingCombo.SelectedValue);
            Db.Execute(@"INSERT INTO OutgoingLetters(OutgoingNumber, OutgoingDate, TargetOrganizationId, CitizenId, Subject, RelatedIncomingId, Priority, Status, FollowUpRequired, CreatedBy) VALUES($num,$date,$org,$citizen,$subject,$related,$pri,$status,$follow,$user)",
                new SqliteParameter("$num", OutgoingNumberBox.Text.Trim()), new SqliteParameter("$date", (OutgoingDatePicker.SelectedDate ?? DateTime.Today).ToString("yyyy-MM-dd")), new SqliteParameter("$org", (object?)orgId ?? DBNull.Value), new SqliteParameter("$citizen", (object?)citizenId ?? DBNull.Value), new SqliteParameter("$subject", OutgoingSubjectBox.Text.Trim()), new SqliteParameter("$related", (object?)relatedIncomingId ?? DBNull.Value), new SqliteParameter("$pri", ComboText(OutgoingPriorityBox)), new SqliteParameter("$status", ComboText(OutgoingStatusBox)), new SqliteParameter("$follow", OutgoingFollowBox.IsChecked == true ? 1 : 0), new SqliteParameter("$user", AppSession.UserId));
            var newId = Convert.ToInt32(Db.Scalar("SELECT MAX(Id) FROM OutgoingLetters") ?? 0);
            Db.Execute("UPDATE Settings SET SettingValue=$v WHERE SettingKey='LastOutgoingNumber'", new SqliteParameter("$v", OutgoingNumberBox.Text.Trim()));
            if (citizenId != null) Directory.CreateDirectory(Path.Combine(AppPaths.CitizenAttachments, citizenId.Value.ToString(), "Outgoing", newId.ToString()));
            OutgoingSubjectBox.Clear(); OutgoingFollowBox.IsChecked = false; OutgoingRelatedIncomingCombo.SelectedIndex = -1; OutgoingCitizenCombo.SelectedIndex = -1; OutgoingCitizenCombo.Text = string.Empty; LoadAll(); if (citizenId != null) LoadCitizenDossier(citizenId.Value); GenerateOutgoingNumber();
        } catch (Exception ex) { MessageBox.Show(ex.Message, "خطأ حفظ الصادر"); }
    }
    private void AddIncoming_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(IncomingNumberBox.Text) || string.IsNullOrWhiteSpace(IncomingSubjectBox.Text)) { MessageBox.Show("رقم الوارد والموضوع مطلوبان"); return; }
            var orgId = EnsureOrganizationFromCombo(IncomingOrgCombo); var citizenId = EnsureCitizenFromCombo(IncomingCitizenCombo);
            Db.Execute(@"INSERT INTO IncomingLetters(IncomingNumber, IncomingDate, SourceOrganizationId, CitizenId, Subject, Priority, Status, FollowUpRequired, CreatedBy) VALUES($num,$date,$org,$citizen,$subject,$pri,$status,$follow,$user)",
                new SqliteParameter("$num", IncomingNumberBox.Text.Trim()), new SqliteParameter("$date", (IncomingDatePicker.SelectedDate ?? DateTime.Today).ToString("yyyy-MM-dd")), new SqliteParameter("$org", (object?)orgId ?? DBNull.Value), new SqliteParameter("$citizen", (object?)citizenId ?? DBNull.Value), new SqliteParameter("$subject", IncomingSubjectBox.Text.Trim()), new SqliteParameter("$pri", ComboText(IncomingPriorityBox)), new SqliteParameter("$status", ComboText(IncomingStatusBox)), new SqliteParameter("$follow", IncomingFollowBox.IsChecked == true ? 1 : 0), new SqliteParameter("$user", AppSession.UserId));
            var newId = Convert.ToInt32(Db.Scalar("SELECT MAX(Id) FROM IncomingLetters") ?? 0);
            Db.Execute("UPDATE Settings SET SettingValue=$v WHERE SettingKey='LastIncomingNumber'", new SqliteParameter("$v", IncomingNumberBox.Text.Trim()));
            if (citizenId != null) Directory.CreateDirectory(Path.Combine(AppPaths.CitizenAttachments, citizenId.Value.ToString(), "Incoming", newId.ToString()));
            IncomingSubjectBox.Clear(); IncomingFollowBox.IsChecked = false; IncomingCitizenCombo.SelectedIndex = -1; IncomingCitizenCombo.Text = string.Empty; LoadAll(); if (citizenId != null) LoadCitizenDossier(citizenId.Value); GenerateIncomingNumber();
        } catch (Exception ex) { MessageBox.Show(ex.Message, "خطأ حفظ الوارد"); }
    }
    private void AddCitizen_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CitizenNameBox.Text) || string.IsNullOrWhiteSpace(CitizenSubjectBox.Text)) { MessageBox.Show("اسم المواطن وموضوع الطلب مطلوبان"); return; }
        Db.Execute(@"INSERT INTO Citizens(FullName,Phone,Address,RequestSubject,Status,FollowUpRequired,CreatedBy) VALUES($n,$p,$a,$s,$st,$f,$u)", new SqliteParameter("$n", CitizenNameBox.Text.Trim()), new SqliteParameter("$p", CitizenPhoneBox.Text.Trim()), new SqliteParameter("$a", CitizenAddressBox.Text.Trim()), new SqliteParameter("$s", CitizenSubjectBox.Text.Trim()), new SqliteParameter("$st", ComboText(CitizenStatusBox)), new SqliteParameter("$f", CitizenFollowBox.IsChecked == true ? 1 : 0), new SqliteParameter("$u", AppSession.UserId));
        CitizenNameBox.Clear(); CitizenPhoneBox.Clear(); CitizenAddressBox.Clear(); CitizenSubjectBox.Clear(); CitizenFollowBox.IsChecked = false; LoadAll();
    }
    private void AddFollowup_Click(object sender, RoutedEventArgs e)
    {
        var typeAr = ComboText(FollowEntityTypeBox); var type = typeAr == "صادر" ? "Outgoing" : typeAr == "وارد" ? "Incoming" : "Citizen";
        if (!int.TryParse(FollowEntityIdBox.Text, out var id) || string.IsNullOrWhiteSpace(FollowActionBox.Text)) { MessageBox.Show("أدخل Id والإجراء"); return; }
        Db.Execute("INSERT INTO Followups(EntityType,EntityId,FollowupDate,ActionTaken,NextActionDate,Status,CreatedBy) VALUES($t,$id,$d,$a,$n,'مفتوح',$u)", new SqliteParameter("$t", type), new SqliteParameter("$id", id), new SqliteParameter("$d", DateTime.Today.ToString("yyyy-MM-dd")), new SqliteParameter("$a", FollowActionBox.Text.Trim()), new SqliteParameter("$n", (FollowNextDatePicker.SelectedDate ?? DateTime.Today).ToString("yyyy-MM-dd")), new SqliteParameter("$u", AppSession.UserId));
        FollowActionBox.Clear(); LoadAll();
    }
    private void LinkLetters_Click(object sender, RoutedEventArgs e)
    {
        var outgoingId = ToInt(LinkOutgoingCombo.SelectedValue); var incomingId = ToInt(LinkIncomingCombo.SelectedValue);
        if (outgoingId == null || incomingId == null) { MessageBox.Show("اختر الصادر والوارد"); return; }
        Db.Execute("UPDATE OutgoingLetters SET RelatedIncomingId=$i WHERE Id=$o", new SqliteParameter("$i", incomingId), new SqliteParameter("$o", outgoingId)); LoadOutgoing(); MessageBox.Show("تم الربط");
    }
    private void AddUser_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewUsernameBox.Text) || string.IsNullOrWhiteSpace(NewUserPasswordBox.Password)) { MessageBox.Show("اسم الدخول وكلمة المرور مطلوبان"); return; }
        Db.Execute("INSERT INTO Users(FullName,Username,PasswordHash,Role,IsActive) VALUES($f,$u,$p,$r,1)", new SqliteParameter("$f", NewUserFullNameBox.Text.Trim()), new SqliteParameter("$u", NewUsernameBox.Text.Trim()), new SqliteParameter("$p", PasswordService.Hash(NewUserPasswordBox.Password)), new SqliteParameter("$r", ComboText(NewUserRoleBox))); LoadUsers();
    }

    private bool IsAdmin()
    {
        if (AppSession.Role == "مدير") return true;
        MessageBox.Show("هذه العملية متاحة للمدير فقط");
        return false;
    }

    private object DbNull(int? v) => v == null ? DBNull.Value : v.Value;

    private void UpdateOutgoing_Click(object sender, RoutedEventArgs e)
    {
        if (!IsAdmin()) return;
        var id = SelectedId(OutgoingGrid); if (id == null) return;
        var orgId = EnsureOrganizationFromCombo(OutgoingOrgCombo); var citizenId = EnsureCitizenFromCombo(OutgoingCitizenCombo); var relatedIncomingId = ToInt(OutgoingRelatedIncomingCombo.SelectedValue);
        Db.Execute(@"UPDATE OutgoingLetters SET OutgoingNumber=$num, OutgoingDate=$date, TargetOrganizationId=$org, CitizenId=$citizen, Subject=$subject, RelatedIncomingId=$related, Priority=$pri, Status=$status, FollowUpRequired=$follow, UpdatedAt=CURRENT_TIMESTAMP WHERE Id=$id",
            new SqliteParameter("$num", OutgoingNumberBox.Text.Trim()), new SqliteParameter("$date", (OutgoingDatePicker.SelectedDate ?? DateTime.Today).ToString("yyyy-MM-dd")), new SqliteParameter("$org", DbNull(orgId)), new SqliteParameter("$citizen", DbNull(citizenId)), new SqliteParameter("$subject", OutgoingSubjectBox.Text.Trim()), new SqliteParameter("$related", DbNull(relatedIncomingId)), new SqliteParameter("$pri", ComboText(OutgoingPriorityBox)), new SqliteParameter("$status", ComboText(OutgoingStatusBox)), new SqliteParameter("$follow", OutgoingFollowBox.IsChecked == true ? 1 : 0), new SqliteParameter("$id", id.Value));
        LoadAll(); MessageBox.Show("تم تعديل الصادر");
    }

    private void UpdateIncoming_Click(object sender, RoutedEventArgs e)
    {
        if (!IsAdmin()) return;
        var id = SelectedId(IncomingGrid); if (id == null) return;
        var orgId = EnsureOrganizationFromCombo(IncomingOrgCombo); var citizenId = EnsureCitizenFromCombo(IncomingCitizenCombo);
        Db.Execute(@"UPDATE IncomingLetters SET IncomingNumber=$num, IncomingDate=$date, SourceOrganizationId=$org, CitizenId=$citizen, Subject=$subject, Priority=$pri, Status=$status, FollowUpRequired=$follow, UpdatedAt=CURRENT_TIMESTAMP WHERE Id=$id",
            new SqliteParameter("$num", IncomingNumberBox.Text.Trim()), new SqliteParameter("$date", (IncomingDatePicker.SelectedDate ?? DateTime.Today).ToString("yyyy-MM-dd")), new SqliteParameter("$org", DbNull(orgId)), new SqliteParameter("$citizen", DbNull(citizenId)), new SqliteParameter("$subject", IncomingSubjectBox.Text.Trim()), new SqliteParameter("$pri", ComboText(IncomingPriorityBox)), new SqliteParameter("$status", ComboText(IncomingStatusBox)), new SqliteParameter("$follow", IncomingFollowBox.IsChecked == true ? 1 : 0), new SqliteParameter("$id", id.Value));
        LoadAll(); MessageBox.Show("تم تعديل الوارد");
    }

    private void DeleteOutgoing_Click(object sender, RoutedEventArgs e)
    {
        if (!IsAdmin()) return;
        var id = SelectedId(OutgoingGrid); if (id == null) return;
        if (MessageBox.Show("حذف الصادر المحدد؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        Db.Execute("DELETE FROM Attachments WHERE EntityType='Outgoing' AND EntityId=$id", new SqliteParameter("$id", id.Value));
        Db.Execute("DELETE FROM Followups WHERE EntityType='Outgoing' AND EntityId=$id", new SqliteParameter("$id", id.Value));
        Db.Execute("DELETE FROM OutgoingLetters WHERE Id=$id", new SqliteParameter("$id", id.Value));
        LoadAll(); MessageBox.Show("تم حذف الصادر");
    }

    private void DeleteIncoming_Click(object sender, RoutedEventArgs e)
    {
        if (!IsAdmin()) return;
        var id = SelectedId(IncomingGrid); if (id == null) return;
        if (MessageBox.Show("حذف الوارد المحدد؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        Db.Execute("UPDATE OutgoingLetters SET RelatedIncomingId=NULL WHERE RelatedIncomingId=$id", new SqliteParameter("$id", id.Value));
        Db.Execute("DELETE FROM Attachments WHERE EntityType='Incoming' AND EntityId=$id", new SqliteParameter("$id", id.Value));
        Db.Execute("DELETE FROM Followups WHERE EntityType='Incoming' AND EntityId=$id", new SqliteParameter("$id", id.Value));
        Db.Execute("DELETE FROM IncomingLetters WHERE Id=$id", new SqliteParameter("$id", id.Value));
        LoadAll(); MessageBox.Show("تم حذف الوارد");
    }

    private string? RunTesseract(string file)
    {
        try
        {
            var psi = new ProcessStartInfo("tesseract", $"\"{file}\" stdout -l ara+eng --psm 6") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            using var p = Process.Start(psi); if (p == null) return null;
            var output = p.StandardOutput.ReadToEnd(); p.WaitForExit(15000);
            return string.IsNullOrWhiteSpace(output) ? null : output;
        } catch { return null; }
    }

    private static string PickLine(string text, params string[] keys)
    {
        foreach (var line in text.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0))
            if (keys.Any(k => line.Contains(k, StringComparison.OrdinalIgnoreCase))) return line;
        return string.Empty;
    }

    private void ApplyOcrToOutgoing(string text)
    {
        var number = Regex.Match(text, @"(?:الصادر|عدد|العدد)\D{0,12}(\d{1,8})").Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(number)) OutgoingNumberBox.Text = number;
        var subject = PickLine(text, "الموضوع", "م/", "م /", "بخصوص"); if (!string.IsNullOrWhiteSpace(subject)) OutgoingSubjectBox.Text = subject.Replace("الموضوع", "").Replace(":", "").Trim();
        var org = PickLine(text, "إلى", "الى", "وزارة", "محافظة", "هيئة", "جهاز"); if (!string.IsNullOrWhiteSpace(org)) OutgoingOrgCombo.Text = org.Replace("إلى", "").Replace("الى", "").Replace("/", " ").Trim();
    }

    private void ApplyOcrToIncoming(string text)
    {
        var number = Regex.Match(text, @"(?:الوارد|عدد|العدد)\D{0,12}(\d{1,8})").Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(number)) IncomingNumberBox.Text = number;
        var subject = PickLine(text, "الموضوع", "م/", "م /", "بخصوص"); if (!string.IsNullOrWhiteSpace(subject)) IncomingSubjectBox.Text = subject.Replace("الموضوع", "").Replace(":", "").Trim();
        var org = PickLine(text, "من", "وزارة", "محافظة", "هيئة", "جهاز"); if (!string.IsNullOrWhiteSpace(org)) IncomingOrgCombo.Text = org.Replace("من", "").Replace("/", " ").Trim();
    }

    private string AskOcrFileAndRead()
    {
        var dlg = new OpenFileDialog { Title = "اختر صورة كتاب للـ OCR", Filter = "Images|*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.bmp|All files|*.*" };
        if (dlg.ShowDialog() != true) return string.Empty;
        var text = RunTesseract(dlg.FileName);
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("لم يتم العثور على Tesseract OCR على الجهاز أو لم يستخرج نصاً. ثبّت Tesseract مع حزمة ara أو استخدم صورة أوضح.");
            return string.Empty;
        }
        return text;
    }

    private void OcrOutgoing_Click(object sender, RoutedEventArgs e) { var text = AskOcrFileAndRead(); if (!string.IsNullOrWhiteSpace(text)) ApplyOcrToOutgoing(text); }
    private void OcrIncoming_Click(object sender, RoutedEventArgs e) { var text = AskOcrFileAndRead(); if (!string.IsNullOrWhiteSpace(text)) ApplyOcrToIncoming(text); }

    private int? SelectedId(DataGrid grid)
    {
        if (grid.SelectedItem is DataRowView row && row.Row.Table.Columns.Contains("Id")) return ToInt(row["Id"]);
        MessageBox.Show("اختر سجل من الجدول أولاً"); return null;
    }
    private string FolderFor(string entityType, int id)
    {
        if (entityType == "Outgoing")
        {
            var citizenId = ToInt(Db.Scalar("SELECT CitizenId FROM OutgoingLetters WHERE Id=$id", new SqliteParameter("$id", id)));
            return citizenId != null ? Path.Combine(AppPaths.CitizenAttachments, citizenId.Value.ToString(), "Outgoing", id.ToString()) : Path.Combine(AppPaths.OutgoingAttachments, id.ToString());
        }
        if (entityType == "Incoming")
        {
            var citizenId = ToInt(Db.Scalar("SELECT CitizenId FROM IncomingLetters WHERE Id=$id", new SqliteParameter("$id", id)));
            return citizenId != null ? Path.Combine(AppPaths.CitizenAttachments, citizenId.Value.ToString(), "Incoming", id.ToString()) : Path.Combine(AppPaths.IncomingAttachments, id.ToString());
        }
        return Path.Combine(AppPaths.CitizenAttachments, id.ToString());
    }
    private void AttachFile(string entityType, int id)
    {
        var dlg = new OpenFileDialog { Title = "اختر الملف", Filter = "All files|*.*", Multiselect = false };
        if (dlg.ShowDialog() != true) return;
        var folder = FolderFor(entityType, id); Directory.CreateDirectory(folder);
        var target = Path.Combine(folder, Path.GetFileName(dlg.FileName)); File.Copy(dlg.FileName, target, true);
        var info = new FileInfo(target);
        Db.Execute("INSERT INTO Attachments(EntityType,EntityId,FileName,FilePath,FileExtension,FileSize,UploadedBy) VALUES($t,$id,$n,$p,$e,$s,$u)", new SqliteParameter("$t", entityType), new SqliteParameter("$id", id), new SqliteParameter("$n", info.Name), new SqliteParameter("$p", target), new SqliteParameter("$e", info.Extension), new SqliteParameter("$s", info.Length), new SqliteParameter("$u", AppSession.UserId));
        if (entityType == "Citizen") LoadCitizenDossier(id);
        else if (entityType == "Outgoing") { var cid = ToInt(Db.Scalar("SELECT CitizenId FROM OutgoingLetters WHERE Id=$id", new SqliteParameter("$id", id))); if (cid != null) LoadCitizenDossier(cid.Value); }
        else if (entityType == "Incoming") { var cid = ToInt(Db.Scalar("SELECT CitizenId FROM IncomingLetters WHERE Id=$id", new SqliteParameter("$id", id))); if (cid != null) LoadCitizenDossier(cid.Value); }
        LoadCitizens();
        MessageBox.Show("تم إرفاق الملف");
    }
    private void OpenFolder(string entityType, int id)
    {
        var folder = FolderFor(entityType, id); Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
    }
    private void AttachOutgoing_Click(object sender, RoutedEventArgs e) { var id = SelectedId(OutgoingGrid); if (id != null) AttachFile("Outgoing", id.Value); }
    private void AttachIncoming_Click(object sender, RoutedEventArgs e) { var id = SelectedId(IncomingGrid); if (id != null) AttachFile("Incoming", id.Value); }
    private void AttachCitizen_Click(object sender, RoutedEventArgs e) { var id = SelectedId(CitizensGrid); if (id != null) AttachFile("Citizen", id.Value); }
    private void OpenOutgoingAttachments_Click(object sender, RoutedEventArgs e) { var id = SelectedId(OutgoingGrid); if (id != null) OpenFolder("Outgoing", id.Value); }
    private void OpenIncomingAttachments_Click(object sender, RoutedEventArgs e) { var id = SelectedId(IncomingGrid); if (id != null) OpenFolder("Incoming", id.Value); }
    private void OpenCitizenAttachments_Click(object sender, RoutedEventArgs e) { var id = SelectedId(CitizensGrid); if (id != null) OpenFolder("Citizen", id.Value); }
    private void CitizensGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { var id = SelectedIdSilent(CitizensGrid); if (id != null) LoadCitizenDossier(id.Value); else ClearCitizenDossier(); }
    private void CitizensGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) { var id = SelectedId(CitizensGrid); if (id != null) { LoadCitizenDossier(id.Value); OpenFolder("Citizen", id.Value); } }
    private int? SelectedIdSilent(DataGrid grid)
    {
        if (grid.SelectedItem is DataRowView row && row.Row.Table.Columns.Contains("Id")) return ToInt(row["Id"]);
        return null;
    }

    private void OutgoingGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OutgoingGrid.SelectedItem is not DataRowView row) return;
        OutgoingNumberBox.Text = row["رقم الصادر"]?.ToString() ?? string.Empty;
        if (DateTime.TryParse(row["تاريخ الصادر"]?.ToString(), out var d)) OutgoingDatePicker.SelectedDate = d;
        OutgoingOrgCombo.Text = row["الجهة"]?.ToString() ?? string.Empty;
        OutgoingCitizenCombo.Text = row.Row.Table.Columns.Contains("المواطن") ? row["المواطن"]?.ToString() ?? string.Empty : string.Empty;
        OutgoingSubjectBox.Text = row["الموضوع"]?.ToString() ?? string.Empty;
        OutgoingFollowBox.IsChecked = (row["متابعة"]?.ToString() ?? "") == "نعم";
    }

    private void OutgoingGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) { var id = SelectedId(OutgoingGrid); if (id != null) OpenFolder("Outgoing", id.Value); }
    private void IncomingGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IncomingGrid.SelectedItem is not DataRowView row) return;
        IncomingNumberBox.Text = row["رقم الوارد"]?.ToString() ?? string.Empty;
        if (DateTime.TryParse(row["تاريخ الوارد"]?.ToString(), out var d)) IncomingDatePicker.SelectedDate = d;
        IncomingOrgCombo.Text = row["الجهة"]?.ToString() ?? string.Empty;
        IncomingCitizenCombo.Text = row.Row.Table.Columns.Contains("المواطن") ? row["المواطن"]?.ToString() ?? string.Empty : string.Empty;
        IncomingSubjectBox.Text = row["الموضوع"]?.ToString() ?? string.Empty;
        IncomingFollowBox.IsChecked = (row["متابعة"]?.ToString() ?? "") == "نعم";
    }

    private void IncomingGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) { var id = SelectedId(IncomingGrid); if (id != null) OpenFolder("Incoming", id.Value); }

    private void CitizenOutgoingGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var id = SelectedId(CitizenOutgoingGrid); if (id != null) OpenFolder("Outgoing", id.Value);
    }
    private void CitizenIncomingGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var id = SelectedId(CitizenIncomingGrid); if (id != null) OpenFolder("Incoming", id.Value);
    }
    private void CitizenAttachmentsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (CitizenAttachmentsGrid.SelectedItem is DataRowView row && row.Row.Table.Columns.Contains("المسار"))
        {
            var path = row["المسار"]?.ToString();
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
    }

    private void Search_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var q = $"%{SearchBox.Text.Trim()}%";
            SearchGrid.ItemsSource = Db.Query(@"SELECT 'صادر' AS 'النوع', o.Id, o.OutgoingNumber AS 'الرقم', o.OutgoingDate AS 'التاريخ', org.Name AS 'الجهة/الاسم', o.Subject AS 'الموضوع', o.Status AS 'الحالة' FROM OutgoingLetters o LEFT JOIN Organizations org ON org.Id=o.TargetOrganizationId WHERE o.OutgoingNumber LIKE $q OR o.Subject LIKE $q OR IFNULL(org.Name,'') LIKE $q
UNION ALL SELECT 'وارد', i.Id, i.IncomingNumber, i.IncomingDate, org.Name, i.Subject, i.Status FROM IncomingLetters i LEFT JOIN Organizations org ON org.Id=i.SourceOrganizationId WHERE i.IncomingNumber LIKE $q OR i.Subject LIKE $q OR IFNULL(org.Name,'') LIKE $q
UNION ALL SELECT 'مواطن', c.Id, CAST(c.Id AS TEXT), c.CreatedAt, c.FullName, c.RequestSubject, c.Status FROM Citizens c WHERE c.FullName LIKE $q OR c.Phone LIKE $q OR c.RequestSubject LIKE $q ORDER BY 4 DESC", new SqliteParameter("$q", q)).DefaultView;
        } catch (Exception ex) { MessageBox.Show(ex.Message, "خطأ البحث"); }
    }

    private void Backup_Click(object sender, RoutedEventArgs e)
    {
        try { BackupResultText.Text = "تم إنشاء النسخة: " + BackupService.CreateBackup(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "خطأ النسخ الاحتياطي"); }
    }
    private void RestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Title = "اختر نسخة احتياطية", Filter = "SQLite DB|*.db|All files|*.*", InitialDirectory = AppPaths.Backups };
        if (dlg.ShowDialog() != true) return;
        if (MessageBox.Show("سيتم استبدال قاعدة البيانات الحالية. هل تريد الاستمرار؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        BackupService.RestoreBackup(dlg.FileName); BackupResultText.Text = "تمت الاستعادة. أعد تشغيل البرنامج."; MessageBox.Show("تمت الاستعادة. أغلق البرنامج وافتحه من جديد.");
    }


    private string AchievementFolder(int id)
    {
        var folder = Path.Combine(AppPaths.AchievementAttachments, id.ToString());
        Directory.CreateDirectory(folder);
        return folder;
    }

    private string GenerateActivityCode()
    {
        var next = Convert.ToInt32(Db.Scalar("SELECT COALESCE(MAX(Id),0)+1 FROM Achievements") ?? 1);
        return $"ACT-{DateTime.Today:yyyy}-{next:000000}";
    }

    private void AttachAchievementWord_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Title = "اختر ملف Word للنشاط", Filter = "Word files|*.doc;*.docx|All files|*.*" };
        if (dlg.ShowDialog() == true) AchievementWordPathText.Text = dlg.FileName;
    }

    private void AttachAchievementImages_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Title = "اختر صور النشاط", Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*", Multiselect = true };
        if (dlg.ShowDialog() == true)
        {
            var temp = Path.Combine(AppPaths.Root, "TempAchievementImages", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            foreach (var f in dlg.FileNames) File.Copy(f, Path.Combine(temp, Path.GetFileName(f)), true);
            AchievementImagesPathText.Text = temp;
        }
    }

    private void AddAchievement_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ActivityTitleBox.Text)) { MessageBox.Show("عنوان النشاط مطلوب"); return; }
        var orgId = EnsureOrganizationFromCombo(ActivityOrgCombo);
        var code = GenerateActivityCode();
        Db.Execute(@"INSERT INTO Achievements(ActivityCode, ActivityDate, Title, Category, Governorate, District, OrganizationId, Description, Keywords, WordFilePath, ImagesFolder, CreatedBy)
VALUES($code,$date,$title,$cat,$gov,$district,$org,$desc,$keys,'','',$user)",
            new SqliteParameter("$code", code),
            new SqliteParameter("$date", (ActivityDatePicker.SelectedDate ?? DateTime.Today).ToString("yyyy-MM-dd")),
            new SqliteParameter("$title", ActivityTitleBox.Text.Trim()),
            new SqliteParameter("$cat", ComboText(ActivityTypeBox)),
            new SqliteParameter("$gov", ActivityGovernorateBox.Text.Trim()),
            new SqliteParameter("$district", ActivityDistrictBox.Text.Trim()),
            new SqliteParameter("$org", DbNull(orgId)),
            new SqliteParameter("$desc", ActivityDescriptionBox.Text.Trim()),
            new SqliteParameter("$keys", ActivityKeywordsBox.Text.Trim()),
            new SqliteParameter("$user", AppSession.UserId));
        var id = Convert.ToInt32(Db.Scalar("SELECT MAX(Id) FROM Achievements") ?? 0);
        var folder = AchievementFolder(id);
        string wordTarget = string.Empty;
        if (!string.IsNullOrWhiteSpace(AchievementWordPathText.Text) && File.Exists(AchievementWordPathText.Text))
        {
            wordTarget = Path.Combine(folder, Path.GetFileName(AchievementWordPathText.Text));
            File.Copy(AchievementWordPathText.Text, wordTarget, true);
        }
        string imagesTarget = Path.Combine(folder, "Images");
        if (!string.IsNullOrWhiteSpace(AchievementImagesPathText.Text) && Directory.Exists(AchievementImagesPathText.Text))
        {
            Directory.CreateDirectory(imagesTarget);
            foreach (var f in Directory.GetFiles(AchievementImagesPathText.Text)) File.Copy(f, Path.Combine(imagesTarget, Path.GetFileName(f)), true);
        }
        Db.Execute("UPDATE Achievements SET WordFilePath=$w, ImagesFolder=$img WHERE Id=$id", new SqliteParameter("$w", wordTarget), new SqliteParameter("$img", Directory.Exists(imagesTarget) ? imagesTarget : string.Empty), new SqliteParameter("$id", id));
        ActivityTitleBox.Clear(); ActivityDescriptionBox.Clear(); ActivityKeywordsBox.Clear(); AchievementWordPathText.Text = string.Empty; AchievementImagesPathText.Text = string.Empty;
        LoadAchievements(); MessageBox.Show("تم حفظ النشاط");
    }

    private void OpenAchievementFolder_Click(object sender, RoutedEventArgs e)
    {
        var id = SelectedId(AchievementsGrid); if (id != null) Process.Start(new ProcessStartInfo(AchievementFolder(id.Value)) { UseShellExecute = true });
    }
    private void AchievementsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => OpenAchievementFolder_Click(sender, e);

    private void GenerateAchievementReport_Click(object sender, RoutedEventArgs e)
    {
        var from = (AchievementReportFromDate.SelectedDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).ToString("yyyy-MM-dd");
        var to = (AchievementReportToDate.SelectedDate ?? DateTime.Today).ToString("yyyy-MM-dd");
        var table = Db.Query(@"SELECT a.ActivityDate AS 'التاريخ', a.Title AS 'عنوان النشاط', a.Category AS 'النوع', a.Governorate AS 'المحافظة', a.District AS 'القضاء', IFNULL(org.Name,'') AS 'الجهة', a.Description AS 'الوصف'
FROM Achievements a LEFT JOIN Organizations org ON org.Id=a.OrganizationId WHERE a.ActivityDate BETWEEN $from AND $to ORDER BY a.ActivityDate ASC, a.Id ASC", new SqliteParameter("$from", from), new SqliteParameter("$to", to));
        Directory.CreateDirectory(AppPaths.Reports);
        var file = Path.Combine(AppPaths.Reports, $"Achievement_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var byType = Db.Query("SELECT Category AS 'النوع', COUNT(*) AS 'العدد' FROM Achievements WHERE ActivityDate BETWEEN $from AND $to GROUP BY Category ORDER BY COUNT(*) DESC", new SqliteParameter("$from", from), new SqliteParameter("$to", to));
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html lang='ar' dir='rtl'><head><meta charset='utf-8'><title>تقرير المنجز</title><style>body{font-family:Tahoma,Arial;margin:32px;line-height:1.8}h1,h2{text-align:center}table{width:100%;border-collapse:collapse;margin:16px 0}th{background:#111827;color:white}td,th{border:1px solid #888;padding:8px;text-align:right}.card{border:1px solid #ddd;border-radius:10px;padding:12px;margin:12px 0}.muted{color:#555}@media print{button{display:none}}</style></head><body><button onclick='print()'>طباعة</button>");
        sb.Append($"<h1>تقرير منجزات النائب</h1><h2>للفترة من {from} إلى {to}</h2><p class='muted'>تم توليده من منظومة الإدارة البرلمانية الذكية.</p>");
        sb.Append($"<h2>الملخص التنفيذي</h2><p>بلغ عدد النشاطات المسجلة ضمن الفترة المحددة <b>{table.Rows.Count}</b> نشاطاً، موزعة حسب التصنيف والجهات والمحافظات كما موضح أدناه.</p>");
        sb.Append("<h2>جدول التصنيفات</h2><table><tr><th>النوع</th><th>العدد</th></tr>");
        foreach (DataRow r in byType.Rows) sb.Append($"<tr><td>{System.Net.WebUtility.HtmlEncode(r["النوع"].ToString())}</td><td>{r["العدد"]}</td></tr>");
        sb.Append("</table><h2>تفاصيل النشاطات</h2>");
        foreach (DataRow r in table.Rows)
        {
            sb.Append("<div class='card'>");
            sb.Append($"<h3>{System.Net.WebUtility.HtmlEncode(r["عنوان النشاط"].ToString())}</h3>");
            sb.Append($"<p><b>التاريخ:</b> {r["التاريخ"]} | <b>النوع:</b> {System.Net.WebUtility.HtmlEncode(r["النوع"].ToString())} | <b>المحافظة:</b> {System.Net.WebUtility.HtmlEncode(r["المحافظة"].ToString())}</p>");
            sb.Append($"<p><b>الجهة:</b> {System.Net.WebUtility.HtmlEncode(r["الجهة"].ToString())}</p>");
            sb.Append($"<p>{System.Net.WebUtility.HtmlEncode(r["الوصف"].ToString())}</p>");
            sb.Append("</div>");
        }
        sb.Append("</body></html>"); File.WriteAllText(file, sb.ToString(), Encoding.UTF8); Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
        AchievementReportResultText.Text = "تم إنشاء تقرير المنجز: " + file;
    }

    private DataTable ReportQuery(string kind) => kind switch
    {
        "outgoing" => Db.Query("SELECT OutgoingNumber AS 'رقم الصادر', OutgoingDate AS 'التاريخ', Subject AS 'الموضوع', Status AS 'الحالة' FROM OutgoingLetters ORDER BY Id DESC"),
        "incoming" => Db.Query("SELECT IncomingNumber AS 'رقم الوارد', IncomingDate AS 'التاريخ', Subject AS 'الموضوع', Status AS 'الحالة' FROM IncomingLetters ORDER BY Id DESC"),
        "citizens" => Db.Query("SELECT FullName AS 'اسم المواطن', Phone AS 'الهاتف', RequestSubject AS 'موضوع الطلب', Status AS 'الحالة' FROM Citizens ORDER BY Id DESC"),
        _ => Db.Query(@"SELECT 'صادر' AS 'النوع', OutgoingNumber AS 'الرقم', OutgoingDate AS 'التاريخ', Subject AS 'الموضوع', Status AS 'الحالة' FROM OutgoingLetters UNION ALL SELECT 'وارد', IncomingNumber, IncomingDate, Subject, Status FROM IncomingLetters UNION ALL SELECT 'مواطن', CAST(Id AS TEXT), CreatedAt, RequestSubject, Status FROM Citizens ORDER BY 4 DESC")
    };
    private string CreateHtmlReport(string title, DataTable table)
    {
        Directory.CreateDirectory(AppPaths.Reports);
        var file = Path.Combine(AppPaths.Reports, $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html lang='ar' dir='rtl'><head><meta charset='utf-8'><title>").Append(title).Append("</title><style>body{font-family:Tahoma,Arial;margin:30px}h1{text-align:center}table{width:100%;border-collapse:collapse}th{background:#111827;color:white}td,th{border:1px solid #999;padding:8px;text-align:right}tr:nth-child(even){background:#f3f4f6}@media print{button{display:none}}</style></head><body><button onclick='print()'>طباعة</button><h1>").Append(title).Append("</h1><table><tr>");
        foreach (DataColumn c in table.Columns) sb.Append("<th>").Append(System.Net.WebUtility.HtmlEncode(c.ColumnName)).Append("</th>");
        sb.Append("</tr>");
        foreach (DataRow r in table.Rows) { sb.Append("<tr>"); foreach (var v in r.ItemArray) sb.Append("<td>").Append(System.Net.WebUtility.HtmlEncode(v?.ToString())).Append("</td>"); sb.Append("</tr>"); }
        sb.Append("</table></body></html>"); File.WriteAllText(file, sb.ToString(), Encoding.UTF8); Process.Start(new ProcessStartInfo(file) { UseShellExecute = true }); return file;
    }
    private void RunReport(string kind, string title) { try { ReportResultText.Text = "تم إنشاء التقرير: " + CreateHtmlReport(title, ReportQuery(kind)); } catch (Exception ex) { MessageBox.Show(ex.Message, "خطأ التقرير"); } }
    private void ReportOutgoing_Click(object sender, RoutedEventArgs e) => RunReport("outgoing", "تقرير الصادر");
    private void ReportIncoming_Click(object sender, RoutedEventArgs e) => RunReport("incoming", "تقرير الوارد");
    private void ReportCitizens_Click(object sender, RoutedEventArgs e) => RunReport("citizens", "تقرير المواطنين");
    private void ReportAll_Click(object sender, RoutedEventArgs e) => RunReport("all", "التقرير الشامل");
}
