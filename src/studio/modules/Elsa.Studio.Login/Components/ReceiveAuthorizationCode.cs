using Elsa.Studio.Components;
using Elsa.Studio.Login.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Login.Components;

/// <summary>
/// Receives the OIDC authorization code and trades it for access_token
/// </summary>
[Route("/signin-oidc")]
/// <summary>
/// Represents the receive authorization code.
/// </summary>
public sealed class ReceiveAuthorizationCode : StudioComponentBase
{
    /// <summary>
    /// Gets or sets the <see cref="NavigationManager"/>.
    /// </summary>
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "code")] private string? AuthorizationCode { get; set; }

    [SupplyParameterFromQuery(Name = "state")] private string? State { get; set; }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (AuthorizationCode != null)
        {
            await AuthorizationService.ReceiveAuthorizationCode(AuthorizationCode, State, default);
        }
    }
}
