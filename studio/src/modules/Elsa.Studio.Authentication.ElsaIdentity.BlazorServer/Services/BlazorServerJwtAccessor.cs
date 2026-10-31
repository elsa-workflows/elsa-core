using Blazored.LocalStorage;
using Elsa.Studio.Authentication.ElsaIdentity.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Studio.Authentication.ElsaIdentity.BlazorServer.Services;

/// <summary>
/// Blazor Server implementation of JWT accessor with prerendering support.
/// </summary>
public class BlazorServerJwtAccessor(IHttpContextAccessor httpContextAccessor, ILocalStorageService localStorageService) : JwtAccessorBase(localStorageService)
{
    /// <inheritdoc />
    protected override bool CanAccessStorage() => !IsPrerendering();

    private bool IsPrerendering() => httpContextAccessor.HttpContext?.Response.HasStarted == false;
}
