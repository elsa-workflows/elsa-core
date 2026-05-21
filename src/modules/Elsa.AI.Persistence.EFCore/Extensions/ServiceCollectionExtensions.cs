using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Persistence.EFCore.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.AI.Persistence.EFCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAiPersistenceStores(this IServiceCollection services, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        if (!services.Any(x => x.ServiceType == typeof(DbContextOptions<AiDbContext>)))
        {
            if (configureDbContext == null)
                throw new InvalidOperationException($"Register {nameof(DbContextOptions<AiDbContext>)} before calling {nameof(AddAiPersistenceStores)}, or call {nameof(AddAiPersistenceStores)} with a database provider configuration.");
            else
                services.AddDbContext<AiDbContext>(configureDbContext);
        }

        services.TryAddScoped<IAiProposalStore, EFCoreAiProposalStore>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAiAuditEventHandler, EFCoreAiAuditSink>());

        return services;
    }
}
