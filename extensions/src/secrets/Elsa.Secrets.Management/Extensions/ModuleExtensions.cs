using Elsa.Features.Services;
using Elsa.Secrets.Management.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseSecretsManagement(this IModule module, Action<SecretManagementFeature>? setup = null)
    {
        return module.Use(setup);
    }
}