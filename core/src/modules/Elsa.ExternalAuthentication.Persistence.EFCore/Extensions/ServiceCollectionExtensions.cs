using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Persistence.EFCore;
using Elsa.ExternalAuthentication.Persistence.EFCore.Stores;
using Elsa.ExternalAuthentication.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers durable, cross-node external authentication state backed by <c>ExternalAuthenticationElsaDbContext</c>.</summary>
public static class ExternalAuthenticationPersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the in-memory external authentication stores with Entity Framework Core implementations.
    /// </summary>
    /// <remarks>
    /// Every store is a singleton that leases a context from a short-lived scope, so none of them capture
    /// tenant-scoped services. <see cref="EFCoreExternalIdentityProvisioner"/> is the exception: it consumes the
    /// scoped <see cref="Elsa.Identity.Contracts.IUserStore"/> and <see cref="Elsa.Identity.Contracts.IUserProvider"/>,
    /// so it stays scoped.
    /// </remarks>
    public static IServiceCollection AddExternalAuthenticationEntityFrameworkCore(this IServiceCollection services)
    {
        services.TryAddSingleton<ExternalAuthenticationDbContextLeaseFactory>();
        services.TryAddSingleton<IExternalAuthenticationHandleHasher, HmacExternalAuthenticationHandleHasher>();
        services.Replace(ServiceDescriptor.Singleton<IIdentityProviderConnectionStore, EFCoreIdentityProviderConnectionStore>());
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
