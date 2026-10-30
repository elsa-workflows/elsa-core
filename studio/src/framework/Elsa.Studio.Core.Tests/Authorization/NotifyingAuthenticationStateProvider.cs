using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Elsa.Studio.Core.Tests.Authorization;

/// <summary>An authenticated user whose state change can be raised on demand.</summary>
internal sealed class NotifyingAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly AuthenticationState _state = new(new ClaimsPrincipal(new ClaimsIdentity("test")));

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);

    public void Notify() => NotifyAuthenticationStateChanged(Task.FromResult(_state));
}
