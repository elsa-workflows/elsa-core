using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.BlazorWasm.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.Services;

/// <summary>Consumes an exact-origin broker callback and exchanges its one-time completion code using PKCE.</summary>
public sealed class ExternalAuthenticationWasmCallbackService(
    NavigationManager navigationManager,
    IExternalAuthenticationPkceTransactionStore transactionStore,
    IAnonymousBackendApiClientProvider anonymousBackendApiClientProvider,
    ExternalAuthenticationWasmTokenProvider tokenProvider,
    ExternalAuthenticationWasmOptions options)
{
    /// <summary>Completes a callback and returns the previously validated client-local target.</summary>
    public async Task<string> CompleteAsync(Uri callbackUri, CancellationToken cancellationToken = default)
    {
        EnsureExactCallback(callbackUri, options.CallbackPath);
        var query = QueryHelpers.ParseQuery(callbackUri.Query);
        var state = query.TryGetValue("state", out var stateValue) ? stateValue.ToString() : null;
        var code = query.TryGetValue("code", out var codeValue) ? codeValue.ToString() : null;
        var hasError = query.ContainsKey("error");

        if (string.IsNullOrWhiteSpace(state))
            throw new InvalidOperationException("The authentication callback did not contain valid state.");

        var transaction = await transactionStore.TakeAsync(state, cancellationToken);
        if (transaction == null ||
            !string.Equals(transaction.State, state, StringComparison.Ordinal) ||
            transaction.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("The authentication callback state is invalid or has already been consumed.");

        // State is intentionally taken before accepting a provider error or missing code, preventing callback replay.
        if (hasError || string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("The authentication callback did not contain a valid completion code.");

        var api = await anonymousBackendApiClientProvider.GetApiAsync<IExternalAuthenticationBrokerApi>(cancellationToken);
        var tokens = await api.ExchangeAsync(
            new(
                "authorization_code",
                options.ClientId,
                GetExactRegisteredUri(options.CallbackPath).AbsoluteUri,
                code,
                transaction.CodeVerifier),
            authorization: null,
            cancellationToken);
        await tokenProvider.SetAsync(tokens, cancellationToken);
        return ExternalAuthenticationReturnPath.Normalize(transaction.ReturnPath);
    }

    /// <summary>Validates the broker logout callback before returning its only safe destination.</summary>
    public string CompleteLogout(Uri callbackUri)
    {
        EnsureExactCallback(callbackUri, options.LogoutCallbackPath);
        return "/";
    }

    private void EnsureExactCallback(Uri callbackUri, string configuredPath)
    {
        var expected = GetExactRegisteredUri(configuredPath);
        if (!string.Equals(callbackUri.Scheme, expected.Scheme, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(callbackUri.Host, expected.Host, StringComparison.OrdinalIgnoreCase) ||
            callbackUri.Port != expected.Port ||
            !string.Equals(callbackUri.AbsolutePath, expected.AbsolutePath, StringComparison.Ordinal))
            throw new InvalidOperationException("The authentication callback URI does not match this Studio client's exact registered callback URI.");
    }

    private Uri GetExactRegisteredUri(string configuredPath)
    {
        if (ExternalAuthenticationReturnPath.Normalize(configuredPath) != configuredPath)
            throw new InvalidOperationException("External Authentication callback paths must be client-local absolute paths.");

        var callback = navigationManager.ToAbsoluteUri(configuredPath);
        var application = navigationManager.ToAbsoluteUri("/");
        if (!string.Equals(callback.Scheme, application.Scheme, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(callback.Host, application.Host, StringComparison.OrdinalIgnoreCase) ||
            callback.Port != application.Port)
        {
            throw new InvalidOperationException("External Authentication callback URIs must use this Studio origin exactly.");
        }

        return callback;
    }
}
