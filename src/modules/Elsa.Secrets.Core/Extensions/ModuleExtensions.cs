using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Secrets.Features;

namespace Elsa.Secrets.Extensions;

public static class ModuleExtensions
{
    public static IModule UseSecrets(this IModule module, Action<SecretsFeature> setup)
    {
        return module.Use(setup);
    }
}