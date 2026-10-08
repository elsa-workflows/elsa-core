using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Services;

/// <summary>
/// A default implementation of <see cref="IRemoteBackendAccessor"/> that uses the <see cref="BackendOptions"/> to determine the URL of the remote backend.
/// </summary>
public class DefaultRemoteBackendAccessor : IRemoteBackendAccessor
{
    private readonly IOptions<BackendOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRemoteBackendAccessor"/> class.
    /// </summary>
    public DefaultRemoteBackendAccessor(IOptions<BackendOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public RemoteBackend RemoteBackend => new(_options.Value.Url);
}