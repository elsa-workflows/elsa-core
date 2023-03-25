using Elsa.Common.Exceptions;
using Elsa.Common.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Options;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Identity.Features;

/// <summary>
/// Provides identity feature to authenticate & authorize API requests.
/// </summary>
[DependsOn(typeof(SystemClockFeature))]
public class IdentityFeature : FeatureBase
{
    /// <inheritdoc />
    public IdentityFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A delegate to configure <see cref="Options.IdentityOptions"/>.
    /// </summary>
    public IdentityOptions IdentityOptions { get; set; } = new();

    /// <summary>
    /// A delegate to configure <see cref="Options.IdentityTokenOptions"/>.
    /// </summary>
    public IdentityTokenOptions TokenOptions { get; set; } = new();

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
    /// Configures the feature to use <see cref="ConfigurationBasedUserProvider"/>.
    /// </summary>
    public void UseStoreBasedUserProvider() => UserProvider = sp => sp.GetRequiredService<StoreBasedUserProvider>();

    /// <summary>
    /// Configures the feature to use <see cref="ConfigurationBasedUserProvider"/>.
    /// </summary>
    public void UseConfigurationBasedUserProvider() => UserProvider = sp => sp.GetRequiredService<ConfigurationBasedUserProvider>();

    /// <summary>
    /// Configures the feature to use <see cref="AdminUserProvider"/>.
    /// </summary>
    public void UseAdminUserProvider() => UserProvider = sp => sp.GetRequiredService<AdminUserProvider>();

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly(GetType());
    }

    /// <inheritdoc />
    public override void Apply()
    {
        if (string.IsNullOrWhiteSpace(TokenOptions.SigningKey))
            throw new MissingConfigurationException("SigningKey is a required setting for the Identity feature.");

        Services.Configure<IdentityTokenOptions>(options => options.CopyFrom(TokenOptions));

        // Memory stores.
        Services
            .AddMemoryStore<User, MemoryUserStore>()
            .AddMemoryStore<Application, MemoryApplicationStore>()
            .AddMemoryStore<Role, MemoryRoleStore>();

        // User providers.
        Services
            .AddSingleton<AdminUserProvider>()
            .AddSingleton<StoreBasedUserProvider>()
            .AddSingleton<ConfigurationBasedUserProvider>();
        
        // Application providers.
        Services
            .AddSingleton<StoreBasedApplicationProvider>()
            .AddSingleton<ConfigurationBasedApplicationProvider>();

        // Services.
        Services
            .AddSingleton(UserStore)
            .AddSingleton(ApplicationStore)
            .AddSingleton(RoleStore)
            .AddSingleton(UserProvider)
            .AddSingleton<ISecretHasher, DefaultSecretHasher>()
            .AddSingleton<IAccessTokenIssuer, DefaultAccessTokenIssuer>()
            .AddSingleton<IUserCredentialsValidator, DefaultUserCredentialsValidator>()
            .AddSingleton<DefaultApiKeyGeneratorAndParser>()
            .AddSingleton<IApiKeyGenerator>(sp => sp.GetRequiredService<DefaultApiKeyGeneratorAndParser>())
            .AddSingleton<IApiKeyParser>(sp => sp.GetRequiredService<DefaultApiKeyGeneratorAndParser>())
            .AddSingleton<IClientIdGenerator, DefaultClientIdGenerator>()
            ;
    }
}