using MudBlazor;

namespace Elsa.Studio.ExternalAuthentication.Services;

public static class ExternalAuthenticationSessionPresentation
{
    public static string StatusLabel(string status) =>
        string.Equals(status, "active", StringComparison.OrdinalIgnoreCase)
            ? "Active"
            : string.Equals(status, "revoked", StringComparison.OrdinalIgnoreCase)
                ? "Revoked"
                : string.IsNullOrWhiteSpace(status)
                    ? "Unknown"
                    : char.ToUpperInvariant(status[0]) + status[1..].ToLowerInvariant();

    public static Color StatusColor(string status) =>
        string.Equals(status, "active", StringComparison.OrdinalIgnoreCase) ? Color.Success : Color.Default;

    public static string StatusIcon(string status) =>
        string.Equals(status, "active", StringComparison.OrdinalIgnoreCase)
            ? Icons.Material.Outlined.CheckCircleOutline
            : string.Equals(status, "revoked", StringComparison.OrdinalIgnoreCase)
                ? Icons.Material.Outlined.Cancel
                : Icons.Material.Outlined.HelpOutline;
}
