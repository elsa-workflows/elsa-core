using Elsa.Features.Services;
using Elsa.Secrets.JavaScript.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseSecretsJavaScript(this IModule module, Action<SecretsJavaScriptFeature>? configure = null)
    {
        return module.Use(configure);
    }
}
