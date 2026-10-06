using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Options;
using Elsa.Studio.Services;

namespace Elsa.Studio.Host.CustomElements.Services;

/// <summary>
/// A default implementation of <see cref="IRemoteBackendAccessor"/> that uses the <see cref="BackendOptions"/> to determine the URL of the remote backend.
/// </summary>
public class ComponentRemoteBackendAccessor : IRemoteBackendAccessor
{
    private readonly BackendService _backendService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRemoteBackendAccessor"/> class.
    /// </summary>
    public ComponentRemoteBackendAccessor(BackendService backendService)
    {
        _backendService = backendService;
    }

    /// <inheritdoc />
    public RemoteBackend RemoteBackend => new(new(_backendService.RemoteEndpoint));
}