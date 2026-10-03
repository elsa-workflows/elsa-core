using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Components;

/// <summary>
/// Represents the error.
/// </summary>
public partial class Error : ComponentBase
{
    /// <summary>
    /// Gets or sets the exception to display.
    /// </summary>
    [Parameter] public Exception Context { get; set; } = null!;

    private bool _signingIn;

    // The host's sign-in hand-off, the same component the shell shows for an UnauthorizedAccessException.
    private RenderFragment? SignIn => Services.GetService<IUnauthorizedComponentProvider>()?.GetUnauthorizedComponent();
}
