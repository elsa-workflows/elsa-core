using Elsa.Features.Services;
using Elsa.Workflows.Management.BlobStorage.Features;

namespace Elsa.Workflows.Management.BlobStorage.Options;

public static class BlobPayloadStoreExtensions
{
    public static IModule UseBlobPayloadStore(this IModule module, Action<BlobPayloadFeature>? configure = null)
    {
        module.Configure<BlobPayloadFeature>(management =>
        {
            configure?.Invoke(management);
        });
        return module;
    }
}
