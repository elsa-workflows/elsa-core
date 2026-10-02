using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Environments.Services;

/// <summary>
/// An <see cref="IRemoteBackendAccessor"/> that prefers the currently selected environment URL
/// and otherwise uses <see cref="BackendOptions"/>.
/// </summary>
/// <remarks>
/// <see cref="Elsa.Studio.Services.DefaultBackendApiClientProvider"/> reads this URL on every <c>GetApiAsync</c> call,
/// so environment switches reuse the shared client factory and auth pipeline.
/// </remarks>
public class EnvironmentRemoteBackendAccessor(
    IEnvironmentService environmentService,
    IOptions<BackendOptions> options) : IRemoteBackendAccessor
{
    /// <inheritdoc />
    public RemoteBackend RemoteBackend => new(environmentService.CurrentEnvironment?.Url ?? options.Value.Url);
}
