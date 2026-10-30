using Elsa.Studio.Login.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Login.Services;

/// <inheritdoc/>
internal class ElsaIdentityAuthorizationService(NavigationManager navigationManager) : IAuthorizationService
{
    /// <inheritdoc/>
    public Task RedirectToAuthorizationServer()
    {
        var returnUrl = navigationManager.ToBaseRelativePath(navigationManager.Uri);
        var loginUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/login" : $"/login?returnUrl={returnUrl}";
        navigationManager.NavigateTo(loginUrl, true);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ReceiveAuthorizationCode(string code, string? state, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }
}
