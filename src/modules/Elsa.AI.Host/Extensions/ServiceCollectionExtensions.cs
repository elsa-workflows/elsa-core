using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Host.Context;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Services;
using Elsa.AI.Host.Streaming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Elsa.AI.Host.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAIHostServices(this IServiceCollection services, Action<AIHostOptions>? configureOptions = null)
    {
        if (configureOptions != null)
            services.Configure(configureOptions);

        services.AddLogging();
        services.AddOptions<AIHostOptions>();
        services.TryAddSingleton<InMemoryAIConversationStore>();
        services.TryAddSingleton<IAIConversationStore>(sp => sp.GetRequiredService<InMemoryAIConversationStore>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAIContextProvider, WorkflowDefinitionContextProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAIContextProvider, WorkflowInstanceContextProvider>());
        services.TryAddSingleton<AIContextResolver>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, AIContextProviderValidationHostedService>());
        services.TryAddSingleton<AIToolEnablementService>();
        services.TryAddSingleton<IAIToolRegistry, AIToolRegistry>();
        services.TryAddScoped<IAIOrchestrator, AIOrchestrator>();
        services.TryAddSingleton<AIStreamSessionManager>();
        services.TryAddSingleton<AIStreamEventMapper>();
        services.TryAddSingleton<AIAuditSink>();
        services.TryAddSingleton<IAIAuditSink>(sp => sp.GetRequiredService<AIAuditSink>());

        return services;
    }
}
