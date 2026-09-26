namespace Elsa.Studio.Settings.Models;

/// <summary>
/// Describes one contributed settings destination. The descriptor is presentation-only and never persists settings.
/// </summary>
public sealed record SettingsSectionDescriptor(
    string Id,
    string DisplayName,
    string Description,
    string Href,
    string Icon,
    float Order = 0);
