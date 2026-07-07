using Microsoft.Data.Sqlite;
using ParliamentOffice.UI.Data;

namespace ParliamentOffice.UI.Services;

public sealed class AuthService
{
    public bool Login(string username, string password, out int userId, out string fullName, out string role)
    {
        userId = 0;
        fullName = string.Empty;
        role = string.Empty;

        var table = Db.Query(@"SELECT Id, FullName, PasswordHash, Role FROM Users WHERE Username=$u AND IsActive=1 LIMIT 1", new SqliteParameter("$u", username.Trim()));
        if (table.Rows.Count == 0) return false;

        var row = table.Rows[0];
        var hash = row["PasswordHash"].ToString() ?? string.Empty;
        if (!PasswordService.Verify((password ?? string.Empty).Trim(), hash)) return false;

        userId = Convert.ToInt32(row["Id"]);
        fullName = row["FullName"].ToString() ?? string.Empty;
        role = row["Role"].ToString() ?? string.Empty;
        return true;
    }
}
