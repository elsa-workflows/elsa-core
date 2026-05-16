using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Options;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Services;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Extensions;

public static class RelationalStructuredLogsServiceCollectionExtensions
{
    public static IServiceCollection AddRelationalStructuredLogPersistence(this IServiceCollection services, Action<RelationalStructuredLogOptions>? configureOptions = null)
    {
        if (configureOptions != null)
            services.Configure(configureOptions);

        services.AddOptions<RelationalStructuredLogOptions>();
        services.TryAddSingleton<RelationalStructuredLogMapper>();
        services.TryAddSingleton<RelationalStructuredLogSqlBuilder>();
        services.TryAddSingleton<RelationalStructuredLogStore>();
        services.TryAddSingleton<StructuredLogWriteBuffer>();
        services.TryAddSingleton<StructuredLogRetentionService>();
        services.AddSingleton<IStructuredLogStore>(sp => sp.GetRequiredService<StructuredLogWriteBuffer>());
        services.AddSingleton<IStructuredLogWriteBuffer>(sp => sp.GetRequiredService<StructuredLogWriteBuffer>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStructuredLogStorageDiagnostics, StructuredLogWriteBufferStorageDiagnostics>());
        services.AddHostedService(sp => sp.GetRequiredService<StructuredLogWriteBuffer>());

        return services;
    }
}
