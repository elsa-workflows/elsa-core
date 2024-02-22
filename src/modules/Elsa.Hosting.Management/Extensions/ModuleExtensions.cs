using Elsa.Features.Services;
using Elsa.Hosting.Management.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseInstanceManagement(this IModule module, Action<InstanceManagementFeature>? configure = default)
    {
        module.Configure<InstanceManagementFeature>(management =>
        {
            configure?.Invoke(management);
        });
        return module;
    }
}