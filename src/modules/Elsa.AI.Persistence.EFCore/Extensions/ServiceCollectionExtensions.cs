using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Host.Services;
using Elsa.AI.Persistence.EFCore.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.AI.Persistence.EFCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAiPersistenceStores(this IServiceCollection services)
    {
        services.TryAddScoped<IAiProposalStore, EFCoreAiProposalStore>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAiAuditEventHandler, EFCoreAiAuditSink>());

        return services;
    }
}
