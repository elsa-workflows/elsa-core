using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.Authentication.Abstractions.Models;

namespace Elsa.Studio.ExternalAuthentication.Services;

/// <summary>Host-neutral discovery orchestration for the login chooser.</summary>
public interface IExternalAuthenticationLoginCoordinator
{
    /// <summary>Optional host-owned POST target for local credentials. Public browser hosts leave this null.</summary>
    string? LocalLoginAction => null;
    /// <summary>Optional deployment warning shown on the public login surface.</summary>
    string? SecurityWarning => null;
    ValueTask<LoginMethodsResponse> DiscoverAsync(CancellationToken cancellationToken = default);
    Task BeginExternalAsync(LoginMethodDescriptor method, string returnPath, CancellationToken cancellationToken = default);
    Task BeginLocalAsync(string username, string password, string returnPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base implementation that owns safe anonymous discovery. A host supplies the actual navigation and
/// transaction persistence because confidential Server and public WASM clients have different boundaries.
/// </summary>
public abstract class ExternalAuthenticationLoginCoordinator : IExternalAuthenticationLoginCoordinator
{
    private readonly IAnonymousBackendApiClientProvider _anonymousBackendApiClientProvider;
    protected ExternalAuthenticationClientOptions Options { get; }
    public virtual string? LocalLoginAction => null;
    public string? SecurityWarning => Options.SecurityWarning;

    protected ExternalAuthenticationLoginCoordinator(
        IAnonymousBackendApiClientProvider anonymousBackendApiClientProvider,
        ExternalAuthenticationClientOptions options)
    {
        _anonymousBackendApiClientProvider = anonymousBackendApiClientProvider;
        Options = options;
    }

    public async ValueTask<LoginMethodsResponse> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var api = await _anonymousBackendApiClientProvider.GetApiAsync<ILoginMethodsApi>(cancellationToken);
        var response = await api.ListAsync(Options.ClientId, cancellationToken);
        return new(LoginMethodChooserState.Order(response.Methods), response.PreferredMethodKey);
    }

    public abstract Task BeginExternalAsync(LoginMethodDescriptor method, string returnPath, CancellationToken cancellationToken = default);
    public abstract Task BeginLocalAsync(string username, string password, string returnPath, CancellationToken cancellationToken = default);
}
