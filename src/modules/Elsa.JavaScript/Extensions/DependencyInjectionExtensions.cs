using Elsa.JavaScript.Configuration;
using Elsa.ServiceConfiguration.Services;

namespace Elsa.JavaScript.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceConfiguration UseJavaScript(this IServiceConfiguration serviceConfiguration, Action<JavaScriptConfigurator>? configure = default)
    {
        serviceConfiguration.Configure(configure);
        return serviceConfiguration;
    }
}