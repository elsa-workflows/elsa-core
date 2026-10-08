using Elsa.Features.Services;
using Elsa.Secrets.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseSecrets(this IModule module, Action<SecretsFeature>? configure = null)
    {
        return module.Use(configure);
    }
}
