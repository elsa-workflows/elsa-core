using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Persistence.EFCore.Services;
using Elsa.AI.Persistence.EFCore.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Elsa.AI.Persistence.EFCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAIPersistenceStores(this IServiceCollection services, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        if (!services.Any(x => x.ServiceType == typeof(DbContextOptions<AIDbContext>)))
        {
            if (configureDbContext == null)
                throw new InvalidOperationException($"Register {nameof(AIDbContext)} with configured {nameof(DbContextOptions<AIDbContext>)} before calling {nameof(AddAIPersistenceStores)}, or call {nameof(AddAIPersistenceStores)} with a database provider configuration.");
            else
                services.AddDbContext<AIDbContext>(configureDbContext);
        }

        services.TryAddScoped<AIDbContext>();

        services.Replace(ServiceDescriptor.Scoped<IAIConversationStore, EFCoreAIConversationStore>());
        services.TryAddScoped<IAIProposalStore, EFCoreAIProposalStore>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAIAuditEventHandler, EFCoreAIAuditSink>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, EFCoreAIConversationCleanupService>());

        return services;
    }
}
