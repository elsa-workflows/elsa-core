using Elsa.Studio.Authentication.UI.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Authentication.UI.Contracts;

/// <summary>
/// Base class for a component-backed, presentation-only login theme.
/// </summary>
public abstract class LoginThemeComponentBase : ComponentBase
{
    /// <summary>
    /// Gets or sets the shared login content positioned by this theme.
    /// </summary>
    [Parameter, EditorRequired]
    public LoginThemeContext Context { get; set; } = null!;
}
