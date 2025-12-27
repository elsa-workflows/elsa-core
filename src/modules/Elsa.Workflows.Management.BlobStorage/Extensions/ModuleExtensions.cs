using Elsa.Features.Services;
using Elsa.Workflows.Management.BlobStorage.Features;

namespace Elsa.Workflows.Management.BlobStorage.Extensions;

public static class ModuleExtensions
{
    /// <summary>
    /// Adds the workflow management feature to the specified module. 
    /// </summary>
    public static IModule UseBlobPayloadStore(this IModule module, Action<BlobPayloadFeature>? configure = null)
    {
        module.Configure<BlobPayloadFeature>(feature =>
        {            
            configure?.Invoke(feature);
        });

        return module;
    }
}
