namespace ParliamentOffice.UI.Services.Security;

/// <summary>
/// Centralizes role checks used by the WPF screens.
/// </summary>
public sealed class PermissionService
{
    private const string ArabicAdminRole = "مدير";
    private const string LegacyMojibakeAdminRole = "Ù…Ø¯ÙŠØ±";

    /// <summary>
    /// Returns true when the supplied role has administrator privileges.
    /// </summary>
    public bool IsAdmin(string? role)
    {
        var normalizedRole = role?.Trim();
        return string.Equals(normalizedRole, ArabicAdminRole, StringComparison.Ordinal)
            || string.Equals(normalizedRole, LegacyMojibakeAdminRole, StringComparison.Ordinal);
    }
}
