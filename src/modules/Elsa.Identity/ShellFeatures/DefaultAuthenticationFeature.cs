using AspNetCore.Authentication.ApiKey;
using CShells.Features;
using Elsa.Extensions;
using Elsa.Identity.Providers;
using Elsa.Requirements;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
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
    DependsOn = ["Identity"])]
[UsedImplicitly]
public class DefaultAuthenticationFeature : IShellFeature
{
    private const string MultiScheme = "Jwt-or-ApiKey";

    /// <summary>
    /// Gets or sets the API key provider type.
    /// </summary>
    public Type ApiKeyProviderType { get; set; } = typeof(DefaultApiKeyProvider);

    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.ConfigureOptions<ValidateIdentityTokenOptions>();

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
            .AddJwtBearer();

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
            options.AddPolicy(IdentityPolicyNames.SecurityRoot, policy => policy.RequireAuthenticatedUser());
        });
    }
}
