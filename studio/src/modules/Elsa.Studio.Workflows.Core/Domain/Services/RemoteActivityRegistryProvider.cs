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
public class RemoteActivityRegistryProvider(
    IBackendApiClientProvider remoteBackendApiClientProvider,
    IRemoteFeatureProvider? remoteFeatureProvider = null) : IActivityRegistryProvider
{
    /// <inheritdoc />
    public async Task<IEnumerable<ActivityDescriptor>> ListAsync(CancellationToken cancellationToken = default)
    {
        var api = await remoteBackendApiClientProvider.GetApiAsync<IActivityDescriptorsApi>(cancellationToken);
        var request = new ListActivityDescriptorsRequest
        {
            Refresh = true
        };
        var response = await api.ListAsync(request, cancellationToken);
        
        var supportsNativeCompensationDrafts = await NativeCompensationDraftCapability.IsEnabledAsync(remoteFeatureProvider, cancellationToken);

        return supportsNativeCompensationDrafts
            ? response.Items
            : response.Items.Where(x => !string.Equals(x.TypeName, "Elsa.CompensationScope", StringComparison.Ordinal));
    }
}
