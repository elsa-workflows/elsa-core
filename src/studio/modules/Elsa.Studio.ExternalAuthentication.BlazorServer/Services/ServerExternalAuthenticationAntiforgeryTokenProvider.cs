using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace Elsa.Studio.ExternalAuthentication.BlazorServer.Services;

/// <summary>Creates the anti-forgery token used by server-hosted broker forms.</summary>
public sealed class ServerExternalAuthenticationAntiforgeryTokenProvider(
    IAntiforgery antiforgery,
    IHttpContextAccessor httpContextAccessor) : IExternalAuthenticationAntiforgeryTokenProvider
{
    public ExternalAuthenticationAntiforgeryToken? GetToken()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
            return null;

        var tokens = antiforgery.GetAndStoreTokens(context);
        return string.IsNullOrWhiteSpace(tokens.RequestToken)
            ? null
            : new ExternalAuthenticationAntiforgeryToken(tokens.FormFieldName, tokens.RequestToken);
    }
}
