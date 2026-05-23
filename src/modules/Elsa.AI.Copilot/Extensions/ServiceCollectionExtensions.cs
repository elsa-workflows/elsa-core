using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Copilot.Adapters;
using Elsa.AI.Copilot.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.AI.Copilot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCopilotAIProvider(this IServiceCollection services, Action<CopilotOptions>? configureOptions = null)
    {
        if (configureOptions != null)
            services.Configure(configureOptions);

        services.AddOptions<CopilotOptions>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAIProvider, CopilotProvider>());

        return services;
    }
}
