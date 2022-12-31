using Elsa.Features.Services;
using Elsa.Jobs.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseJobs(this IModule module, Action<JobsFeature>? configure = default )
    {
        module.Configure(configure);
        return module;
    }
}