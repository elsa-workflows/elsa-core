using AspNetCore.Authentication.ApiKey;
using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Multitenancy;
using Elsa.Identity.Options;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Identity.ShellFeatures;

/// <summary>
/// Provides identity feature to authenticate &amp; authorize API requests.
/// </summary>
[ShellFeature(
    DisplayName = "Identity",
    Description = "Provides identity management, authentication and authorization capabilities",
    DependsOn = ["SystemClock"])]
[UsedImplicitly]
public class IdentityFeature : IFastEndpointsShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Configure options - Note: SigningKey must be configured by the application for security
        services.AddOptions<IdentityTokenOptions>().BindConfiguration("Identity");
        services.Configure<ApiKeyOptions>(ApiKeyDefaults.AuthenticationScheme, options =>
        {
            options.Realm = "Elsa Workflows";
            options.KeyName = "ApiKey";
        });
        services.Configure<UsersOptions>(_ => { });
        services.Configure<ApplicationsOptions>(_ => { });
        services.Configure<RolesOptions>(_ => { });

        // Memory stores.
        services
            .AddMemoryStore<User, MemoryUserStore>()
            .AddMemoryStore<Application, MemoryApplicationStore>()
            .AddMemoryStore<Role, MemoryRoleStore>();

        // User providers.
        services
            .AddScoped<AdminUserProvider>()
            .AddScoped<StoreBasedUserProvider>()
            .AddScoped<ConfigurationBasedUserProvider>();

        // Application providers.
        services
            .AddScoped<StoreBasedApplicationProvider>()
            .AddScoped<ConfigurationBasedApplicationProvider>();

        // Role providers.
        services
            .AddScoped<AdminRoleProvider>()
            .AddScoped<StoreBasedRoleProvider>()
            .AddScoped<ConfigurationBasedRoleProvider>();

        // Tenant resolution strategies.
        services
            .AddScoped<ITenantResolver, ClaimsTenantResolver>()
            .AddScoped<ITenantResolver, CurrentUserTenantResolver>();

        // Services.
        services
            .AddScoped<IUserManager, UserManager>()
            .AddScoped<IRoleManager, RoleManager>()
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
            .AddHttpContextAccessor()
            ;

        // Overridable services.
        services
            .AddScoped<IUserStore, MemoryUserStore>()
            .AddScoped<IApplicationStore, MemoryApplicationStore>()
            .AddScoped<IRoleStore, MemoryRoleStore>()
            .AddScoped<IUserProvider, StoreBasedUserProvider>()
            .AddScoped<IApplicationProvider, StoreBasedApplicationProvider>()
            .AddScoped<IRoleProvider, StoreBasedRoleProvider>();
    }
}