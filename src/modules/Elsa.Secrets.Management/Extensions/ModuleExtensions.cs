using Elsa.Features.Services;
using Elsa.Secrets.Management.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseSecretsManagement(this IModule module, Action<SecretsManagementFeature> setup)
    {
        return module.Use(setup);
    }
}