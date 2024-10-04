using Elsa.Features.Services;
using Elsa.Secrets.Management.Features;
using Elsa.Secrets.Scripting.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseSecretsScripting(this IModule module, Action<SecretsScriptingFeature>? setup = null)
    {
        return module.Use(setup);
    }
}