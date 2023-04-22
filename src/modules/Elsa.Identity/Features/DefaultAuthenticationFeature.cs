using AspNetCore.Authentication.ApiKey;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Identity.Providers;
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
    }

    /// <summary>
    /// Gets or sets the <see cref="ApiKeyProviderType"/>.
    /// </summary>
    public Type ApiKeyProviderType { get; set; } = typeof(DefaultApiKeyProvider);

    /// <summary>
    /// Configures the API key provider type.
    /// </summary>
    /// <typeparam name="T">The type of the API key provider.</typeparam>
    /// <returns>The current <see cref="DefaultAuthenticationFeature"/>.</returns>
    public DefaultAuthenticationFeature UseApiKeyAuthorization<T>() where T : class, IApiKeyProvider
    {
        _configureApiKeyAuthorization = builder => builder.AddApiKeyInAuthorizationHeader<T>();
        return this;
    }

    /// <summary>
    /// Configures the API key provider type to <see cref="AdminApiKeyProvider"/>.
    /// </summary>
    /// <returns>The current <see cref="DefaultAuthenticationFeature"/>.</returns>
    public DefaultAuthenticationFeature UseAdminApiKey() => UseApiKeyAuthorization<AdminApiKeyProvider>();

    /// <inheritdoc />
    public override void Apply()
    {
        Services.ConfigureOptions<ConfigureJwtBearerOptions>();
        Services.ConfigureOptions<ValidateIdentityTokenOptions>();

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
            .AddJwtBearer();

        _configureApiKeyAuthorization(authBuilder);

        Services.AddSingleton<IAuthorizationHandler, LocalHostRequirementHandler>();
        Services.AddSingleton<IAuthorizationHandler, LocalHostPermissionRequirementHandler>();
        Services.AddSingleton(ApiKeyProviderType);
        Services.AddSingleton<IApiKeyProvider>(sp => (IApiKeyProvider)sp.GetRequiredService(ApiKeyProviderType));
        Services.AddAuthorization(options => options.AddPolicy(IdentityPolicyNames.SecurityRoot, policy => policy.AddRequirements(new LocalHostPermissionRequirement())));
    }
}