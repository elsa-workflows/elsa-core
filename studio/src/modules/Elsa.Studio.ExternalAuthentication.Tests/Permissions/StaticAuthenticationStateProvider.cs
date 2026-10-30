using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Elsa.Studio.ExternalAuthentication.Tests.Permissions;

internal sealed class StaticAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
}
