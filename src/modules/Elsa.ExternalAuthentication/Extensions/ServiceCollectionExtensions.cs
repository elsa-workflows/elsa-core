using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Policies;
using Elsa.ExternalAuthentication.Providers;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Elsa.ExternalAuthentication.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>Adds the explicit, non-readiness External Authentication health bridge.</summary>
    public static IHealthChecksBuilder AddExternalAuthenticationHealthCheck(this IServiceCollection services, string name = "external-authentication", IEnumerable<string>? tags = null) =>
        services.AddHealthChecks().AddCheck<ExternalAuthenticationHealthCheck>(name, HealthStatus.Degraded, tags ?? ["external-authentication", "optional"]);

    /// <summary>
    /// Adds the protocol-neutral External Authentication foundation and its single-node defaults.
    /// Hosts requiring durable, multi-node state may replace the store registrations.
    /// </summary>
    public static IServiceCollection AddExternalAuthenticationServices(this IServiceCollection services, Action<ExternalAuthenticationOptions>? configureOptions = null)
    {
        var options = services.AddOptions<ExternalAuthenticationOptions>().ValidateOnStart();
        if (configureOptions != null)
            options.Configure(configureOptions);

        services.AddExternalAuthenticationExtension(ExternalAuthenticationExtensionKind.UnlinkedIdentityPolicy, RejectUnlinkedIdentityPolicy.PolicyType);
        services.AddExternalAuthenticationExtension(ExternalAuthenticationExtensionKind.UnlinkedIdentityPolicy, CreateUserUnlinkedIdentityPolicy.PolicyType);
        services.AddExternalAuthenticationExtension(ExternalAuthenticationExtensionKind.PermissionGrantSource, ElsaRolePermissionGrantSource.SourceType);
        services.AddExternalAuthenticationExtension(ExternalAuthenticationExtensionKind.PermissionGrantSource, ClaimMappingPermissionGrantSource.SourceType);
        services.AddExternalAuthenticationExtension(ExternalAuthenticationExtensionKind.PermissionGrantSource, GroupMappingPermissionGrantSource.SourceType);
        services.AddExternalAuthenticationExtension(ExternalAuthenticationExtensionKind.PermissionGrantSource, ClaimPassThroughPermissionGrantSource.SourceType);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<ExternalAuthenticationOptions>, ExternalAuthenticationOptionsValidator>());
        services.AddDataProtection();
        services.AddRateLimiter(_ => { });
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<RateLimiterOptions>, ConfigureExternalAuthenticationRateLimiterOptions>());

        services.TryAddSingleton<ConnectionRevisionCalculator>();
        services.TryAddSingleton<FinalLoginPathGuard>();
        services.TryAddSingleton<ExternalAuthenticationSecurityNotifier>();
        services.TryAddScoped<ConnectionTestService>();
        services.TryAddScoped<PreviewSignInService>();
        services.TryAddSingleton<ExternalAuthenticationHealthCheck>();
        services.TryAddSingleton<IOutboundDnsResolver, SystemOutboundDnsResolver>();
        services.TryAddSingleton<OutboundDestinationValidator>();
        services.TryAddSingleton<IValidatedAddressConnector, SocketValidatedAddressConnector>();
        services.TryAddSingleton<ValidatedOutboundConnectionFactory>();
        services.TryAddSingleton<IProviderHttpClientFactory, ProviderHttpClientFactory>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IIdentityProviderConnectionSource, ConfigurationIdentityProviderConnectionSource>());
        services.TryAddSingleton<IIdentityProviderConnectionStore, InMemoryIdentityProviderConnectionStore>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IIdentityProviderConnectionSource, DatabaseIdentityProviderConnectionSource>());
        services.TryAddSingleton<IIdentityProviderConnectionRegistry, DefaultIdentityProviderConnectionRegistry>();
        services.TryAddSingleton<ExtensionDescriptorValidator>();
        services.TryAddSingleton<IExternalAuthenticationAdapterRegistry, DefaultExternalAuthenticationAdapterRegistry>();
        services.TryAddSingleton<IUnlinkedIdentityPolicyRegistry, DefaultUnlinkedIdentityPolicyRegistry>();
        services.TryAddScoped<IPermissionGrantSourceRegistry, DefaultPermissionGrantSourceRegistry>();
        services.TryAddSingleton<IAdapterSettingsMigrationService, AdapterSettingsMigrationService>();

        services.TryAddSingleton<IExternalAuthenticationStateStore, InMemoryExternalAuthenticationStateStore>();
        services.TryAddSingleton<IExternalAuthenticationHandleHasher, HmacExternalAuthenticationHandleHasher>();
        services.TryAddSingleton<IAuthorizationGrantStore, InMemoryAuthorizationGrantStore>();
        services.TryAddSingleton<IExternalAuthenticationSessionStore, InMemoryExternalAuthenticationSessionStore>();
        services.TryAddSingleton<IPreviewResultStore, InMemoryPreviewResultStore>();
        services.TryAddSingleton<IConnectionObservationStore, InMemoryConnectionObservationStore>();
        services.TryAddSingleton<IConnectionRegistryVersionStore, InMemoryConnectionRegistryVersionStore>();
        services.TryAddSingleton<InMemoryExternalIdentityProvisionerState>();
        services.TryAddScoped<InMemoryExternalIdentityProvisioner>();
        services.TryAddScoped<IExternalIdentityProvisioner>(serviceProvider => serviceProvider.GetRequiredService<InMemoryExternalIdentityProvisioner>());
        services.TryAddScoped<IExternalIdentityLinkManagementStore>(serviceProvider => serviceProvider.GetRequiredService<InMemoryExternalIdentityProvisioner>());
        services.TryAddScoped<ExternalIdentityLinkManagementService>();
        services.TryAddScoped<IExternalIdentityResolver, DefaultExternalIdentityResolver>();
        services.TryAddScoped<IPermissionGrantResolver, DefaultPermissionGrantResolver>();
        services.TryAddScoped<IPermissionDelegationAuthorizer, DefaultPermissionDelegationAuthorizer>();
        services.TryAddScoped<IPermissionDescriptorRegistry, DefaultPermissionDescriptorRegistry>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IUnlinkedIdentityPolicy, RejectUnlinkedIdentityPolicy>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IUnlinkedIdentityPolicy, CreateUserUnlinkedIdentityPolicy>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IPermissionGrantSource, ElsaRolePermissionGrantSource>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IPermissionGrantSource, ClaimMappingPermissionGrantSource>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IPermissionGrantSource, GroupMappingPermissionGrantSource>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IPermissionGrantSource, ClaimPassThroughPermissionGrantSource>());
        services.TryAddScoped<IExternalAuthenticationTokenIssuer, DefaultExternalAuthenticationTokenIssuer>();
        services.TryAddScoped<IExternalAuthenticationBroker, ExternalAuthenticationBroker>();
        services.TryAddScoped<IdentityProviderConnectionManagementService>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPermissionDescriptorProvider, ExternalAuthenticationPermissionDescriptorProvider>());

        return services;
    }

    /// <summary>
    /// Registers the stable identifier of a trusted deployment-installed extension
    /// for startup selection validation.
    /// </summary>
    public static IServiceCollection AddExternalAuthenticationExtension(
        this IServiceCollection services,
        ExternalAuthenticationExtensionKind kind,
        string type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        services.Configure<ExternalAuthenticationExtensionOptions>(options =>
            options.Registrations.Add(new ExternalAuthenticationExtensionRegistration(kind, type)));
        return services;
    }
}
