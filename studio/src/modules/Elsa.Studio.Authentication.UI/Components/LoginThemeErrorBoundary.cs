using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Elsa.Studio.Authentication.UI.Components;

/// <summary>
/// Records render failures from a selected theme before the recovery shell is displayed.
/// </summary>
public sealed class LoginThemeErrorBoundary : ErrorBoundary
{
    [Inject] private ILogger<LoginThemeErrorBoundary> Logger { get; set; } = null!;

    /// <summary>
    /// Gets or sets the stable ID of the selected theme.
    /// </summary>
    [Parameter, EditorRequired]
    public string ThemeId { get; set; } = null!;

    protected override Task OnErrorAsync(Exception exception)
    {
        Logger.LogError(exception, "Login theme {ThemeId} failed while rendering; displaying the recovery shell.", ThemeId);
        return Task.CompletedTask;
    }
}
