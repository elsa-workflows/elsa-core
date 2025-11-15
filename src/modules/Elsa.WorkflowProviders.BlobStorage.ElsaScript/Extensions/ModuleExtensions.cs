using Elsa.Features.Services;
using Elsa.WorkflowProviders.BlobStorage.ElsaScript.Features;

namespace Elsa.WorkflowProviders.BlobStorage.ElsaScript.Extensions;

/// <summary>
/// Extensions for <see cref="IModule"/>.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Enables ElsaScript support for BlobStorage workflows.
    /// </summary>
    public static IModule UseElsaScriptBlobStorage(this IModule module, Action<ElsaScriptBlobStorageFeature>? configure = null)
    {
        module.Configure(configure);
        return module;
    }
}
