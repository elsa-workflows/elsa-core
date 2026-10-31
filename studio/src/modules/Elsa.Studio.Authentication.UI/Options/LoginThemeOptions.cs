using Elsa.Studio.Authentication.UI.Models;

namespace Elsa.Studio.Authentication.UI.Options;

/// <summary>
/// Selects the login presentation registered by the application.
/// </summary>
public sealed class LoginThemeOptions
{
    /// <summary>
    /// The configuration section consumed by the core login theme framework.
    /// </summary>
    public const string SectionName = "Authentication:Login";

    /// <summary>
    /// Gets or sets the stable identifier of the selected login theme. Use
    /// <c>inherit</c> to follow the application-wide presentation theme.
    /// </summary>
    public string Theme { get; set; } = LoginThemeIds.Classic;
}
