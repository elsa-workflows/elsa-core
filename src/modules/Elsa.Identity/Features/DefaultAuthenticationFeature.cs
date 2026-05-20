using AspNetCore.Authentication.ApiKey;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Identity.Constants;
using Elsa.Identity.Options;
using Elsa.Identity.Providers;
using Elsa.Options;
using Elsa.Requirements;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Identity.Features;

/// <summary>
/// Provides an authorization feature that configures the system with JWT bearer and API key authentication.
/// </summary>
[DependsOn(typeof(IdentityFeature))]
public class DefaultAuthenticationFeature : FeatureBase
{
    private const string MultiScheme = "Jwt-or-ApiKey";
    private Func<AuthenticationBuilder, AuthenticationBuilder> _configureApiKeyAuthorization = builder => builder.AddApiKeyInAuthorizationHeader<DefaultApiKeyProvider>();

    /// <inheritdoc />
    public DefaultAuthenticationFeature(IModule module) : base(module)
    {
        ConfigureAuthorizationOptions = ConfigureDefaultSecurityRootPolicy;
    }

    /// <summary>
    /// Gets or sets the <see cref="ApiKeyProviderType"/>.
    /// </summary>
    public Type ApiKeyProviderType { get; set; } = typeof(DefaultApiKeyProvider);
    public Action<AuthorizationOptions> ConfigureAuthorizationOptions { get; set; }

    /// <summary>
    /// Gets or sets whether localhost requests may satisfy the security-root permission requirement without other credentials.
    /// </summary>
    public bool EnableLocalHostPermissionGrant { get; set; }

    /// <summary>
    /// Configures the API key provider type.
    /// </summary>
    /// <typeparam name="T">The type of the API key provider.</typeparam>
    /// <returns>The current <see cref="DefaultAuthenticationFeature"/>.</returns>
    public DefaultAuthenticationFeature UseApiKeyAuthorization<T>() where T : class, IApiKeyProvider
    {
        ApiKeyProviderType = typeof(T);
        _configureApiKeyAuthorization = builder => builder.AddApiKeyInAuthorizationHeader<T>();
        return this;
    }

    /// <summary>
    /// Configures the API key provider type to <see cref="AdminApiKeyProvider"/>. The provider denies all keys unless configured.
    /// </summary>
    /// <returns>The current <see cref="DefaultAuthenticationFeature"/>.</returns>
    public DefaultAuthenticationFeature UseAdminApiKey() => UseApiKeyAuthorization<AdminApiKeyProvider>();

    /// <summary>
    /// Configures the admin API key provider with an explicit API key.
    /// </summary>
    /// <param name="apiKey">The API key to accept.</param>
    /// <returns>The current <see cref="DefaultAuthenticationFeature"/>.</returns>
    public DefaultAuthenticationFeature UseAdminApiKey(string apiKey)
    {
        Services.Configure<AdminApiKeyOptions>(options => options.ApiKey = apiKey);
        return UseAdminApiKey();
    }

    /// <summary>
    /// Configures the admin API key provider with an explicit API key.
    /// </summary>
    /// <param name="configure">The admin API key options to configure.</param>
    /// <returns>The current <see cref="DefaultAuthenticationFeature"/>.</returns>
    public DefaultAuthenticationFeature UseAdminApiKey(Action<AdminApiKeyOptions> configure)
    {
        Services.Configure(configure);
        return UseAdminApiKey();
    }

    /// <summary>
    /// Enables the all-zero development admin API key. Do not use in production.
    /// </summary>
    /// <returns>The current <see cref="DefaultAuthenticationFeature"/>.</returns>
    public DefaultAuthenticationFeature UseDevelopmentAdminApiKey() => UseAdminApiKey(AdminApiKeyProvider.DevelopmentApiKey);

    /// <summary>
    /// Enables the legacy localhost permission grant for the security root policy.
    /// </summary>
    public DefaultAuthenticationFeature EnableLocalHostPermissionGrantForSecurityRoot()
    {
        EnableLocalHostPermissionGrant = true;
        return this;
    }

    /// <summary>
    /// Disables the localhost permission grant for the security root policy.
    /// This is useful when privileged identity bootstrap is handled through features such as <see cref="DefaultAdminUserFeature"/>.
    /// </summary>
    public DefaultAuthenticationFeature DisableLocalHostPermissionGrantForSecurityRoot()
    {
        EnableLocalHostPermissionGrant = false;
        return this;
    }

    /// <summary>
    /// Disables the legacy localhost permission grant for the security root policy.
    /// This is useful when privileged identity bootstrap is handled through features such as <see cref="DefaultAdminUserFeature"/>.
    /// </summary>
    [Obsolete("Use DisableLocalHostPermissionGrantForSecurityRoot instead.")]
    public DefaultAuthenticationFeature DisableLocalHostRequirement() => DisableLocalHostPermissionGrantForSecurityRoot();

    /// <inheritdoc />
    public override void Apply()
    {
        Services.ConfigureOptions<ConfigureJwtBearerOptions>();
        Services.Configure<AdminApiKeyOptions>(_ => { });
        Services.AddIdentityTokenOptionsValidation();
        Services.Configure<LocalHostPermissionRequirementOptions>(options => options.EnableLocalHostPermissionGrant = EnableLocalHostPermissionGrant);

        var authBuilder = Services
            .AddAuthentication(MultiScheme)
            .AddPolicyScheme(MultiScheme, MultiScheme, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    return context.Request.Headers.Authorization.Any(x => x!.Contains(ApiKeyDefaults.AuthenticationScheme))
                        ? ApiKeyDefaults.AuthenticationScheme
                        : JwtBearerDefaults.AuthenticationScheme;
                };
            })
            .AddJwtBearer()
            .AddJwtBearer(IdentityAuthenticationSchemes.RefreshToken);

        _configureApiKeyAuthorization(authBuilder);

        Services.AddScoped<IAuthorizationHandler, LocalHostRequirementHandler>();
        Services.AddScoped<IAuthorizationHandler, LocalHostPermissionRequirementHandler>();
        Services.AddScoped(ApiKeyProviderType);
        Services.AddScoped<IApiKeyProvider>(sp => (IApiKeyProvider)sp.GetRequiredService(ApiKeyProviderType));
        Services.AddAuthorization(ConfigureAuthorizationOptions);
    }

    private static void ConfigureAuthenticatedSecurityRootPolicy(AuthorizationOptions options)
    {
        options.AddPolicy(IdentityPolicyNames.SecurityRoot, policy => policy.RequireAuthenticatedUser());
    }

    private void ConfigureDefaultSecurityRootPolicy(AuthorizationOptions options)
    {
        if (EnableLocalHostPermissionGrant)
            ConfigureLocalHostSecurityRootPolicy(options);
        else
            ConfigureAuthenticatedSecurityRootPolicy(options);
    }

    private static void ConfigureLocalHostSecurityRootPolicy(AuthorizationOptions options)
    {
        options.AddPolicy(IdentityPolicyNames.SecurityRoot, policy => policy.AddRequirements(new LocalHostPermissionRequirement()));
    }
}
