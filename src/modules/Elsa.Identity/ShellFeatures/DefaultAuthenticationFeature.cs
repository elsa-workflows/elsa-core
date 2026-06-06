using AspNetCore.Authentication.ApiKey;
using CShells.Features;
using Elsa.Extensions;
using Elsa.Identity.Constants;
using Elsa.Identity.Options;
using Elsa.Identity.Providers;
using Elsa.Options;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.Requirements;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Identity.ShellFeatures;

/// <summary>
/// Provides an authorization feature that configures the system with JWT bearer and API key authentication.
/// </summary>
[ShellFeature(
    DisplayName = "Default Authentication",
    Description = "Provides JWT bearer and API key authentication",
    DependsOn = [typeof(global::Elsa.Identity.ShellFeatures.IdentityFeature)])]
[UsedImplicitly]
public class DefaultAuthenticationFeature : IShellFeature
{
    private const string MultiScheme = "Jwt-or-ApiKey";

    /// <summary>
    /// Gets or sets the API key provider type.
    /// </summary>
    public Type ApiKeyProviderType { get; set; } = typeof(DefaultApiKeyProvider);

    /// <summary>
    /// Gets or sets an explicit API key for <see cref="AdminApiKeyProvider"/>. Leave empty to disable the provider.
    /// </summary>
    [ManifestSetting(
        DisplayName = "Admin API Key",
        Description = "Explicit API key for the admin API key provider. Leave empty to disable built-in admin API key authentication.",
        Category = "Security",
        Secret = true,
        Sensitive = true,
        RestartRequired = true)]
    public string AdminApiKey { get; set; } = "";

    /// <summary>
    /// Gets or sets whether the all-zero development admin API key should be enabled. Do not enable in production.
    /// </summary>
    [ManifestSetting(
        DisplayName = "Use Development Admin API Key",
        Description = "Enables the all-zero development admin API key. Do not enable in production.",
        Category = "Security",
        DefaultValue = "false",
        RestartRequired = true)]
    public bool UseDevelopmentAdminApiKey { get; set; }

    /// <summary>
    /// Gets or sets whether localhost requests may satisfy the security-root permission requirement without other credentials.
    /// </summary>
    public bool EnableLocalHostPermissionGrant { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        var resolvedAdminApiKey = UseDevelopmentAdminApiKey ? AdminApiKeyProvider.DevelopmentApiKey : AdminApiKey;
        if (!string.IsNullOrWhiteSpace(resolvedAdminApiKey))
            ApiKeyProviderType = typeof(AdminApiKeyProvider);

        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddIdentityTokenOptionsValidation();
        services.Configure<LocalHostPermissionRequirementOptions>(options => options.EnableLocalHostPermissionGrant = EnableLocalHostPermissionGrant);
        services.Configure<AdminApiKeyOptions>(options =>
        {
            options.ApiKey = resolvedAdminApiKey;
        });

        var authBuilder = services
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

        // Configure API key authorization based on provider type
        if (ApiKeyProviderType == typeof(AdminApiKeyProvider))
            authBuilder.AddApiKeyInAuthorizationHeader<AdminApiKeyProvider>();
        else
            authBuilder.AddApiKeyInAuthorizationHeader<DefaultApiKeyProvider>();

        services.AddScoped<IAuthorizationHandler, LocalHostRequirementHandler>();
        services.AddScoped<IAuthorizationHandler, LocalHostPermissionRequirementHandler>();
        services.AddScoped(ApiKeyProviderType);
        services.AddScoped<IApiKeyProvider>(sp => (IApiKeyProvider)sp.GetRequiredService(ApiKeyProviderType));

        services.AddAuthorization(options =>
        {
            if (EnableLocalHostPermissionGrant)
                options.AddPolicy(IdentityPolicyNames.SecurityRoot, policy => policy.AddRequirements(new LocalHostPermissionRequirement()));
            else
                options.AddPolicy(IdentityPolicyNames.SecurityRoot, policy => policy.RequireAuthenticatedUser());
        });
    }
}
