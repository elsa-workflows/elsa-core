using Elsa.Api.Client.Resources.Package.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// Provides information about a remote server.
/// </summary>
public class RemoteServerInformationProvider : IServerInformationProvider
{
    private readonly IBackendApiClientProvider _backendApiClientProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteServerInformationProvider"/> class.
    /// </summary>
    public RemoteServerInformationProvider(IBackendApiClientProvider backendApiClientProvider)
    {
        _backendApiClientProvider = backendApiClientProvider;
    }
    
    /// <inheritdoc />
    public async ValueTask<ServerInformation> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        var api = await _backendApiClientProvider.GetApiAsync<IPackageApi>(cancellationToken);
        var response = await api.GetAsync(cancellationToken);
        var packageVersion = response.PackageVersion.ToString();

        return new (PackageVersion: packageVersion);
    }
}