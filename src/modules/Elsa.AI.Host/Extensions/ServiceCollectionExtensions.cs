using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Host.Context;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Services;
using Elsa.AI.Host.Streaming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.AI.Host.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAiHostServices(this IServiceCollection services, Action<AiHostOptions>? configureOptions = null)
    {
        if (configureOptions != null)
            services.Configure(configureOptions);

        services.AddOptions<AiHostOptions>();
        services.TryAddSingleton<InMemoryAiConversationStore>();
        services.TryAddSingleton<IAiConversationStore>(sp => sp.GetRequiredService<InMemoryAiConversationStore>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAiContextProvider, WorkflowDefinitionContextProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAiContextProvider, WorkflowInstanceContextProvider>());
        services.TryAddSingleton<AiContextResolver>();
        services.TryAddSingleton<AiToolEnablementService>();
        services.TryAddSingleton<IAiToolRegistry, AiToolRegistry>();
        services.TryAddSingleton<IAiOrchestrator, AiOrchestrator>();
        services.TryAddSingleton<AiStreamSessionManager>();
        services.TryAddSingleton<AiStreamEventMapper>();
        services.TryAddSingleton<AiAuditSink>();
        services.TryAddSingleton<IAiAuditSink>(sp => sp.GetRequiredService<AiAuditSink>());

        return services;
    }
}
