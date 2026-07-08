using System.Security.Cryptography;
using System.Text;

namespace ParliamentOffice.UI.Services;

public static class PasswordService
{
    public static string Hash(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public static bool Verify(string password, string hash)
    {
        password = (password ?? string.Empty).Trim();
        hash = (hash ?? string.Empty).Trim();

        // يدعم القواعد القديمة التي خُزنت فيها كلمة المرور كنص صريح بالخطأ
        if (password.Equals(hash, StringComparison.Ordinal)) return true;

        return Hash(password).Equals(hash, StringComparison.OrdinalIgnoreCase);
    }
}
