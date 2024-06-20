using AspNetCore.Authentication.ApiKey;
using Elsa.Common.Contracts;
using Elsa.Common.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.MultiTenancy;
using Elsa.Identity.Options;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Identity.Features;

/// <summary>
/// Provides identity feature to authenticate & authorize API requests.
/// </summary>
[DependsOn(typeof(SystemClockFeature))]
[PublicAPI]
public class IdentityFeature : FeatureBase
{
    /// <inheritdoc />
    public IdentityFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Gets or sets the <see cref="IdentityTokenOptions"/>.
    /// </summary>
    public Action<IdentityTokenOptions> TokenOptions { get; set; } = _ => { };

    /// <summary>
    /// Gets or sets the <see cref="ApiKeyOptions"/>.
    /// </summary>
    public Action<ApiKeyOptions> ApiKeyOptions { get; set; } = options =>
    {
        options.Realm = "Elsa Workflows";
        options.KeyName = "ApiKey";
    };

    /// <summary>
    /// A delegate that configures the <see cref="UsersOptions"/>.
    /// </summary>
    public Action<UsersOptions> UsersOptions { get; set; } = _ => { };

    /// <summary>
    /// A delegate that configures the <see cref="ApplicationsOptions"/>.
    /// </summary>
    public Action<ApplicationsOptions> ApplicationsOptions { get; set; } = _ => { };

    /// <summary>
    /// A delegate that configures the <see cref="RolesOptions"/>.
    /// </summary>
    public Action<RolesOptions> RolesOptions { get; set; } = _ => { };

    /// <summary>
    /// A delegate that creates an instance of an implementation of <see cref="IUserStore"/>.
    /// </summary>
    public Func<IServiceProvider, IUserStore> UserStore { get; set; } = sp => sp.GetRequiredService<MemoryUserStore>();

    /// <summary>
    /// A delegate that creates an instance of an implementation of <see cref="IApplicationStore"/>.
    /// </summary>
    public Func<IServiceProvider, IApplicationStore> ApplicationStore { get; set; } = sp => sp.GetRequiredService<MemoryApplicationStore>();

    /// <summary>
    /// A delegate that creates an instance of an implementation of <see cref="IRoleStore"/>.
    /// </summary>
    public Func<IServiceProvider, IRoleStore> RoleStore { get; set; } = sp => sp.GetRequiredService<MemoryRoleStore>();

    /// <summary>
    /// A delegate that creates an instance of an implementation of <see cref="IUserProvider"/>.
    /// </summary>
    public Func<IServiceProvider, IUserProvider> UserProvider { get; set; } = sp => sp.GetRequiredService<StoreBasedUserProvider>();

    /// <summary>
    /// A delegate that creates an instance of an implementation of <see cref="IApplicationProvider"/>.
    /// </summary>
    public Func<IServiceProvider, IApplicationProvider> ApplicationProvider { get; set; } = sp => sp.GetRequiredService<StoreBasedApplicationProvider>();

    /// <summary>
    /// A delegate that creates an instance of an implementation of <see cref="IRoleProvider"/>.
    /// </summary>
    public Func<IServiceProvider, IRoleProvider> RoleProvider { get; set; } = sp => sp.GetRequiredService<StoreBasedRoleProvider>();

    /// <summary>
    /// Configures the feature to use <see cref="ConfigurationBasedUserProvider"/>.
    /// </summary>
    public void UseStoreBasedUserProvider() => UserProvider = sp => sp.GetRequiredService<StoreBasedUserProvider>();

    /// <summary>
    /// Configures the feature to use <see cref="ConfigurationBasedUserProvider"/>.
    /// </summary>
    public void UseConfigurationBasedUserProvider(Action<UsersOptions> configure)
    {
        UserProvider = sp => sp.GetRequiredService<ConfigurationBasedUserProvider>();
        UsersOptions += configure;
    }

    /// <summary>
    /// Configures the feature to use <see cref="AdminUserProvider"/>.
    /// </summary>
    public void UseAdminUserProvider()
    {
        UserProvider = sp => sp.GetRequiredService<AdminUserProvider>();
        RoleProvider = sp => sp.GetRequiredService<AdminRoleProvider>();
    }

    /// <summary>
    /// Configures the feature to use <see cref="StoreBasedApplicationProvider"/>.
    /// </summary>
    public void UseStoreBasedApplicationProvider() => ApplicationProvider = sp => sp.GetRequiredService<StoreBasedApplicationProvider>();

    /// <summary>
    /// Configures the feature to use <see cref="ConfigurationBasedApplicationProvider"/>.
    /// </summary>
    public void UseConfigurationBasedApplicationProvider(Action<ApplicationsOptions> configure)
    {
        ApplicationProvider = sp => sp.GetRequiredService<ConfigurationBasedApplicationProvider>();
        ApplicationsOptions += configure;
    }

    /// <summary>
    /// Configures the feature to use <see cref="StoreBasedRoleProvider"/>.
    /// </summary>
    public void UseStoreBasedRoleProvider() => RoleProvider = sp => sp.GetRequiredService<StoreBasedRoleProvider>();

    /// <summary>
    /// Configures the feature to use <see cref="ConfigurationBasedRoleProvider"/>.
    /// </summary>
    public void UseConfigurationBasedRoleProvider(Action<RolesOptions> configure)
    {
        RoleProvider = sp => sp.GetRequiredService<ConfigurationBasedRoleProvider>();
        RolesOptions += configure;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly(GetType());
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(TokenOptions);
        Services.Configure(ApiKeyDefaults.AuthenticationScheme, ApiKeyOptions);
        Services.Configure(UsersOptions);
        Services.Configure(ApplicationsOptions);
        Services.Configure(RolesOptions);

        // Memory stores.
        Services
            .AddMemoryStore<User, MemoryUserStore>()
            .AddMemoryStore<Application, MemoryApplicationStore>()
            .AddMemoryStore<Role, MemoryRoleStore>();

        // User providers.
        Services
            .AddScoped<AdminUserProvider>()
            .AddScoped<StoreBasedUserProvider>()
            .AddScoped<ConfigurationBasedUserProvider>();

        // Application providers.
        Services
            .AddScoped<StoreBasedApplicationProvider>()
            .AddScoped<ConfigurationBasedApplicationProvider>();

        // Role providers.
        Services
            .AddScoped<AdminRoleProvider>()
            .AddScoped<StoreBasedRoleProvider>()
            .AddScoped<ConfigurationBasedRoleProvider>();

        // Tenant resolution strategies.
        Services
            .AddScoped<ITenantResolutionStrategy, ClaimsTenantResolver>()
            .AddScoped<ITenantResolutionStrategy, CurrentUserTenantResolver>();

        // Services.
        Services
            .AddScoped(UserStore)
            .AddScoped(ApplicationStore)
            .AddScoped(RoleStore)
            .AddScoped(UserProvider)
            .AddScoped(ApplicationProvider)
            .AddScoped(RoleProvider)
            .AddScoped<ISecretHasher, DefaultSecretHasher>()
            .AddScoped<IAccessTokenIssuer, DefaultAccessTokenIssuer>()
            .AddScoped<IUserCredentialsValidator, DefaultUserCredentialsValidator>()
            .AddScoped<IApplicationCredentialsValidator, DefaultApplicationCredentialsValidator>()
            .AddScoped<IApiKeyGenerator>(sp => sp.GetRequiredService<DefaultApiKeyGeneratorAndParser>())
            .AddScoped<IApiKeyParser>(sp => sp.GetRequiredService<DefaultApiKeyGeneratorAndParser>())
            .AddScoped<IClientIdGenerator, DefaultClientIdGenerator>()
            .AddScoped<ISecretGenerator, DefaultSecretGenerator>()
            .AddScoped<IRandomStringGenerator, DefaultRandomStringGenerator>()
            .AddScoped<DefaultApiKeyGeneratorAndParser>()
            ;
    }
}