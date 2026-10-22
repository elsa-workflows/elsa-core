using Elsa.Api.Client.Resources.ActivityDescriptors.Contracts;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Requests;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// An activity registry provider that uses a remote backend to retrieve activity descriptors.
/// </summary>
public class RemoteActivityRegistryProvider : IActivityRegistryProvider
{
    private readonly IBackendApiClientProvider remoteBackendApiClientProvider;
    private readonly IRemoteFeatureProvider? remoteFeatureProvider;

    public RemoteActivityRegistryProvider(IBackendApiClientProvider remoteBackendApiClientProvider)
        : this(remoteBackendApiClientProvider, null)
    {
    }

    public RemoteActivityRegistryProvider(IBackendApiClientProvider remoteBackendApiClientProvider, IRemoteFeatureProvider? remoteFeatureProvider)
    {
        this.remoteBackendApiClientProvider = remoteBackendApiClientProvider;
        this.remoteFeatureProvider = remoteFeatureProvider;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityDescriptor>> ListAsync(CancellationToken cancellationToken = default)
    {
        var backendContextKey = remoteBackendApiClientProvider.ContextKey;
        var api = await remoteBackendApiClientProvider.GetApiAsync<IActivityDescriptorsApi>(cancellationToken);
        EnsureSameBackendContext(backendContextKey, cancellationToken);
        var request = new ListActivityDescriptorsRequest
        {
            Refresh = true
        };
        var response = await api.ListAsync(request, cancellationToken);
        EnsureSameBackendContext(backendContextKey, cancellationToken);

        var supportsNativeCompensationDrafts = await NativeCompensationDraftCapability.IsEnabledForContextAsync(
            remoteFeatureProvider, remoteBackendApiClientProvider, backendContextKey, cancellationToken);
        EnsureSameBackendContext(backendContextKey, cancellationToken);

        return supportsNativeCompensationDrafts
            ? response.Items
            : response.Items.Where(x => !string.Equals(x.TypeName, "Elsa.CompensationScope", StringComparison.Ordinal));
    }

    private void EnsureSameBackendContext(string expected, CancellationToken cancellationToken)
    {
        if (!string.Equals(remoteBackendApiClientProvider.ContextKey, expected, StringComparison.Ordinal))
            throw new OperationCanceledException("The selected backend changed while loading activity descriptors.", cancellationToken);
    }
}
