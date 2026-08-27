using Elsa.Extensions;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Policies;
using Elsa.ExternalAuthentication.Providers;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Elsa.ExternalAuthentication.Validation;
using Elsa.Identity.Contracts;
using Microsoft.AspNetCore.Builder;
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

        // The module evaluates permissions outside endpoint authorization -- delegation, the grant boundary,
        // and the recovery override -- so it depends on the evaluator whether or not a host wired one up.
        // The call is TryAdd-based and idempotent, so a host that already registered one keeps it.
        services.AddElsaAuthorization();

        // Contributed explicitly rather than left to the host's assembly scan, so the module's resources reach
        // the catalog on any host that registers its services, the same reason AddElsaAuthorization is called
        // here. Registration is TryAddEnumerable-backed, so a host that also scans this assembly gets one copy.
        services.AddPermissionDescriptors<ExternalAuthenticationResourcePermissionsDescriptorProvider>();

        services.AddExternalAuthenticationExtension(ExternalAuthenticationExtensionKind.UnlinkedIdentityPolicy, RejectUnlinkedIdentityPolicy.PolicyType);
        services.AddExternalAuthenticationExtension(ExternalAuthenticationExtensionKind.UnlinkedIdentityPolicy, CreateUserUnlinkedIdentityPolicy.PolicyType);
        services.AddExternalAuthenticationExtension(ExternalAuthenticationExtensionKind.UnlinkedIdentityPolicy, MatchExternalUserUnlinkedIdentityPolicy.PolicyType);
        services.AddExternalAuthenticationExtension(ExternalAuthenticationExtensionKind.PermissionGrantSource, ElsaRolePermissionGrantSource.SourceType);
        services.AddExternalAuthenticationExtension(ExternalAuthenticationExtensionKind.PermissionGrantSource, ClaimMappingPermissionGrantSource.SourceType);
        services.AddExternalAuthenticationExtension(ExternalAuthenticationExtensionKind.PermissionGrantSource, GroupMappingPermissionGrantSource.SourceType);
        services.AddExternalAuthenticationExtension(ExternalAuthenticationExtensionKind.PermissionGrantSource, ClaimPassThroughPermissionGrantSource.SourceType);
        // The validator warns about grant-boundary configuration, and ValidateOnStart resolves it on any
        // IOptions access, so a logger has to be resolvable even on a bare service collection. AddLogging is
        // TryAdd-based, so a host that already configured logging keeps its own.
        services.AddLogging();
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
        services.TryAddSingleton<IIdentityProviderConnectionValidityAssessor, IdentityProviderConnectionValidityAssessor>();
        services.TryAddSingleton<ExtensionDescriptorValidator>();
        services.TryAddSingleton<IExternalAuthenticationAdapterRegistry, DefaultExternalAuthenticationAdapterRegistry>();
        services.TryAddSingleton<IUnlinkedIdentityPolicyRegistry, DefaultUnlinkedIdentityPolicyRegistry>();
        services.TryAddSingleton<IExternalUserMatcherRegistry, DefaultExternalUserMatcherRegistry>();
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
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISecretBindingResolver, ConfigurationSecretBindingResolver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IUnlinkedIdentityPolicy, RejectUnlinkedIdentityPolicy>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IUnlinkedIdentityPolicy, CreateUserUnlinkedIdentityPolicy>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IUnlinkedIdentityPolicy, MatchExternalUserUnlinkedIdentityPolicy>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IPermissionGrantSource, ElsaRolePermissionGrantSource>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IPermissionGrantSource, ClaimMappingPermissionGrantSource>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IPermissionGrantSource, GroupMappingPermissionGrantSource>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IPermissionGrantSource, ClaimPassThroughPermissionGrantSource>());
        services.TryAddScoped<IExternalAuthenticationTokenIssuer, DefaultExternalAuthenticationTokenIssuer>();
        services.TryAddScoped<IExternalAuthenticationBroker, ExternalAuthenticationBroker>();
        services.TryAddScoped<IdentityProviderConnectionManagementService>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IRoleDeletionDependencyContributor, ExternalAuthenticationRoleDeletionDependencyContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDeletionDependencyContributor, ExternalAuthenticationUserDeletionDependencyContributor>());

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
            options.Registrations.Add(new(kind, type)));
        return services;
    }
}
