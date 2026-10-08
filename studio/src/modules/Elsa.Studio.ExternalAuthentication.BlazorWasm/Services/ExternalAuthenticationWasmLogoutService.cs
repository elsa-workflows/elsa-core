using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.BlazorWasm.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.Services;

/// <summary>Ends the local session and optionally follows a server-authorized upstream sign-out continuation.</summary>
public sealed class ExternalAuthenticationWasmLogoutService(
    IAnonymousBackendApiClientProvider anonymousBackendApiClientProvider,
    ExternalAuthenticationWasmTokenProvider tokenProvider,
    NavigationManager navigationManager,
    ExternalAuthenticationWasmOptions options)
{
    /// <summary>Logs out locally, or starts an approved upstream flow when requested and supported by the connection.</summary>
    public async Task LogoutAsync(string mode = "local", CancellationToken cancellationToken = default)
    {
        if (mode is not ("local" or "upstream"))
            throw new ArgumentOutOfRangeException(nameof(mode), "Logout mode must be 'local' or 'upstream'.");

        BrokerLogoutResponse? response = null;
        try
        {
            var accessToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken))
                return;

            var api = await anonymousBackendApiClientProvider.GetApiAsync<IExternalAuthenticationBrokerApi>(cancellationToken);
            response = await api.LogoutAsync(
                new(options.ClientId, GetExactLogoutCallback().AbsoluteUri, mode),
                $"Bearer {accessToken}",
                cancellationToken);
        }
        finally
        {
            // Local sign-out is never contingent on an upstream capability or network success.
            await tokenProvider.ClearAsync(cancellationToken);
        }

        if (!response?.Completed ?? false)
        {
            var continuation = response?.NavigationUrl;
            if (string.IsNullOrWhiteSpace(continuation))
                return;

            var uri = GetTrustedBrokerContinuation(continuation);
            navigationManager.NavigateTo(uri.AbsoluteUri, forceLoad: true);
        }
    }

    private Uri GetExactLogoutCallback()
    {
        if (ExternalAuthenticationReturnPath.Normalize(options.LogoutCallbackPath) != options.LogoutCallbackPath)
            throw new InvalidOperationException("External Authentication logout callback paths must be client-local absolute paths.");

        return navigationManager.ToAbsoluteUri(options.LogoutCallbackPath);
    }

    private Uri GetTrustedBrokerContinuation(string value)
    {
        if (!BackendUriResolver.TryResolveSameOrigin(
                anonymousBackendApiClientProvider.Url,
                value,
                out var uri) ||
            !uri.AbsolutePath.Contains("/external-authentication/logout/continue/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The broker returned an invalid logout continuation URI.");
        }

        return uri;
    }
}
