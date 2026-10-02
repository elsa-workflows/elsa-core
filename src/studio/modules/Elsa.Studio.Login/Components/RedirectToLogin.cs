using Elsa.Studio.Login.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Login.Components;

/// <summary>
/// Redirects to the login page.
/// </summary>
public class RedirectToLogin : ComponentBase
{
    /// <summary>
    /// Gets or sets the <see cref="AuthorizationService"/>.
    /// </summary>
    [Inject] protected IAuthorizationService AuthorizationService { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await AuthorizationService.RedirectToAuthorizationServer();
    }
}