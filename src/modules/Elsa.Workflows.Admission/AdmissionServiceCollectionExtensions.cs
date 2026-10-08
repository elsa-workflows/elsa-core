using Elsa.Connections.Contracts;
using Elsa.Connections.Services;
using Elsa.Tenants;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Elsa.Workflows.Admission;

/// <summary>Shared registration for classic and ShellFeatures hosts. Persistence is selected separately.</summary>
public static class AdmissionServiceCollectionExtensions
{
    public static IServiceCollection AddDurableWorkflowAdmission(this IServiceCollection services, AdmissionHostConfiguration configuration)
    {
        configuration.BindRegistrations(services);
        services.AddSingleton(configuration);
        services.TryAddSingleton<AdmissionAuthorityRegistry>();
        services.AddScoped<IWorkflowExecutionGuard, AdmissionExecutionGuard>();
        services.AddScoped(sp => new AdmissionExecutionService(sp.GetRequiredService<IAdmissionStore>(), sp.GetRequiredService<AdmissionAuthorityRegistry>(),
            sp, sp.GetRequiredService<IWorkflowDefinitionService>(), sp.GetRequiredService<IWorkflowInstanceManager>(), sp.GetRequiredService<IWorkflowRunner>(),
            sp.GetRequiredService<IWorkflowStateSerializer>(), sp.GetRequiredService<IWorkflowStateExtractor>(), sp.GetRequiredService<IActivitySerializer>(),
            sp.GetRequiredService<IPayloadSerializer>(), sp.GetRequiredService<IBookmarkStore>(), sp.GetRequiredService<ISystemClock>(),
            sp.GetRequiredService<ITenantAccessor>(), configuration));
        services.Replace(ServiceDescriptor.Scoped<IWorkflowRuntime, AdmissionWorkflowRuntime>());
        services.AddScoped<AdmissionBootstrapService>();
        services.Replace(ServiceDescriptor.Scoped<IWorkflowDefinitionPublisher, AdmissionDeniedManagement>());
        services.Replace(ServiceDescriptor.Scoped<IWorkflowDefinitionManager, AdmissionDeniedManagement>());
        services.RemoveAll<DefaultConnectionLifecycleService>();
        services.Replace(ServiceDescriptor.Scoped<IConnectionLifecycleService, AdmissionDeniedConnectionManagement>());
        services.Replace(ServiceDescriptor.Scoped<IStaticApiKeyLifecycleService, AdmissionDeniedConnectionManagement>());
        services.Replace(ServiceDescriptor.Scoped<IConnectionLifecycleRecoveryService, AdmissionDeniedConnectionManagement>());
        services.Replace(ServiceDescriptor.Scoped<IConnectionBackgroundUseService, AdmissionDeniedConnectionManagement>());
        if (services.Any(x => x.ServiceType == typeof(ITenantStore)))
        {
            services.Decorate<ITenantStore, AdmissionTenantStore>();
        }
        services.AddHostedService<AdmissionHostValidator>();
        return services;
    }

    private sealed class AdmissionHostValidator(IServiceScopeFactory scopes) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<AdmissionHostConfiguration>().ValidateAsync(scope.ServiceProvider, cancellationToken);
        }
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
