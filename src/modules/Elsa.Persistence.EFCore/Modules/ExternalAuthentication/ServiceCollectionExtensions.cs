using Elsa.ExternalAuthentication.Contracts;
using Elsa.Persistence.EFCore.Modules.ExternalAuthentication;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers durable, cross-node external authentication state backed by <c>IdentityElsaDbContext</c>.</summary>
public static class ExternalAuthenticationPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddExternalAuthenticationEntityFrameworkCore(this IServiceCollection services)
    {
        services.TryAddSingleton<ExternalAuthenticationDbContextFactory>();
        services.Replace(ServiceDescriptor.Scoped<IIdentityProviderConnectionStore, EFCoreIdentityProviderConnectionStore>());
        services.Replace(ServiceDescriptor.Scoped(typeof(EFCoreExternalIdentityProvisioner), typeof(EFCoreExternalIdentityProvisioner)));
        services.Replace(ServiceDescriptor.Scoped<IExternalIdentityProvisioner>(serviceProvider => serviceProvider.GetRequiredService<EFCoreExternalIdentityProvisioner>()));
        services.Replace(ServiceDescriptor.Scoped<IExternalIdentityLinkManagementStore>(serviceProvider => serviceProvider.GetRequiredService<EFCoreExternalIdentityProvisioner>()));
        services.Replace(ServiceDescriptor.Singleton<IExternalAuthenticationStateStore, EFCoreExternalAuthenticationStateStore>());
        services.Replace(ServiceDescriptor.Singleton<IAuthorizationGrantStore, EFCoreAuthorizationGrantStore>());
        services.Replace(ServiceDescriptor.Singleton<IExternalAuthenticationSessionStore, EFCoreExternalAuthenticationSessionStore>());
        services.Replace(ServiceDescriptor.Singleton<IPreviewResultStore, EFCorePreviewResultStore>());
        services.Replace(ServiceDescriptor.Singleton<IConnectionObservationStore, EFCoreConnectionObservationStore>());
        services.Replace(ServiceDescriptor.Singleton<IConnectionRegistryVersionStore, EFCoreConnectionRegistryVersionStore>());
        return services;
    }
}
