using Elsa.Studio.Contracts;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.BlazorWasm.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.Services;

/// <summary>Public-client login orchestration. It only navigates to the configured broker origin and exact callback.</summary>
public sealed class ExternalAuthenticationWasmLoginCoordinator : ExternalAuthenticationLoginCoordinator
{
    private readonly IAnonymousBackendApiClientProvider anonymousBackendApiClientProvider;
    private readonly IExternalAuthenticationPkceService pkceService;
    private readonly NavigationManager navigationManager;
    private readonly ExternalAuthenticationWasmOptions options;

    /// <summary>Creates the public-client broker login coordinator.</summary>
    public ExternalAuthenticationWasmLoginCoordinator(
        IAnonymousBackendApiClientProvider anonymousBackendApiClientProvider,
        IExternalAuthenticationPkceService pkceService,
        NavigationManager navigationManager,
        ExternalAuthenticationWasmOptions options)
        : base(anonymousBackendApiClientProvider, options.ToClientOptions())
    {
        this.anonymousBackendApiClientProvider = anonymousBackendApiClientProvider;
        this.pkceService = pkceService;
        this.navigationManager = navigationManager;
        this.options = options;
    }

    /// <inheritdoc />
    public override async Task BeginExternalAsync(LoginMethodDescriptor method, string returnPath, CancellationToken cancellationToken = default)
    {
        var (transaction, challenge) = await pkceService.CreateAsync(returnPath, cancellationToken);
        var initiationUri = GetTrustedBrokerUri(method.InitiationUri);
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = options.ClientId,
            ["redirect_uri"] = GetExactCallbackUri().AbsoluteUri,
            ["response_type"] = "code",
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["return_path"] = transaction.ReturnPath,
            ["state"] = transaction.State
        };
        navigationManager.NavigateTo(QueryHelpers.AddQueryString(initiationUri.AbsoluteUri, query));
    }

    /// <inheritdoc />
    public override async Task BeginLocalAsync(string username, string password, string returnPath, CancellationToken cancellationToken = default)
    {
        var (transaction, challenge) = await pkceService.CreateAsync(returnPath, cancellationToken);
        var api = await anonymousBackendApiClientProvider.GetApiAsync<IExternalAuthenticationBrokerApi>(cancellationToken);
        var response = await api.AuthorizeLocalAsync(
            new(
                options.ClientId,
                GetExactCallbackUri().AbsoluteUri,
                "code",
                challenge,
                "S256",
                transaction.ReturnPath,
                username,
                password,
                transaction.State),
            cancellationToken);
        var callback = new Uri(response.RedirectUri, UriKind.Absolute);
        EnsureExactCallback(callback);
        navigationManager.NavigateTo(callback.AbsoluteUri);
    }

    private Uri GetExactCallbackUri()
    {
        if (ExternalAuthenticationReturnPath.Normalize(options.CallbackPath) != options.CallbackPath)
            throw new InvalidOperationException("External Authentication callback paths must be client-local absolute paths.");

        return navigationManager.ToAbsoluteUri(options.CallbackPath);
    }

    private Uri GetTrustedBrokerUri(string value)
    {
        if (!BackendUriResolver.TryResolveSameOrigin(
                anonymousBackendApiClientProvider.Url,
                value,
                out var uri))
        {
            throw new InvalidOperationException("The broker returned an initiation URI outside the configured Elsa backend origin.");
        }

        return uri;
    }

    private void EnsureExactCallback(Uri callback)
    {
        var expected = GetExactCallbackUri();
        if (!BackendUriResolver.IsSameOrigin(callback, expected) ||
            !string.Equals(callback.AbsolutePath, expected.AbsolutePath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The broker did not return this Studio client's exact registered callback URI.");
        }
    }
}
