using AspNetCore.Authentication.ApiKey;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Identity.Providers;
using Elsa.Requirements;
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

    /// <inheritdoc />
    public DefaultAuthenticationFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.ConfigureOptions<ConfigureJwtBearerOptions>();
        Services.ConfigureOptions<ValidateIdentityTokenOptions>();

        Services
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
            .AddApiKeyInAuthorizationHeader<DefaultApiKeyProvider>();

        Services.AddSingleton<IAuthorizationHandler, LocalHostRequirementHandler>();
        Services.AddAuthorization(options => options.AddPolicy(IdentityPolicyNames.SecurityRoot, policy => policy.AddRequirements(new LocalHostRequirement())));
    }
}