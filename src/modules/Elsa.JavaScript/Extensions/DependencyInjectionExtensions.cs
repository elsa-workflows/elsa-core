using Elsa.Features.Services;
using Elsa.JavaScript.Features;

namespace Elsa.JavaScript.Extensions;

public static class DependencyInjectionExtensions
{
    public static IModule UseJavaScript(this IModule module, Action<JavaScriptFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}