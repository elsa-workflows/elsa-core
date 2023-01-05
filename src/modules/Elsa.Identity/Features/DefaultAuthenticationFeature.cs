using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Requirements;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Identity.Features;

/// <summary>
/// Provides an authorization feature that configures the system with JWT bearer authentication.
/// </summary>
[DependsOn(typeof(IdentityFeature))]
public class DefaultAuthenticationFeature : FeatureBase
{
    /// <inheritdoc />
    public DefaultAuthenticationFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        var identityFeature = Module.Configure<IdentityFeature>();
        var identityOptions = identityFeature.TokenOptions;
        
        Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, identityOptions.ConfigureJwtBearerOptions);
        
        Services.AddSingleton<IAuthorizationHandler, LocalHostRequirementHandler>();
        Services.AddAuthorization(options => options.AddPolicy(IdentityPolicyNames.SecurityRoot, policy => policy.AddRequirements(new LocalHostRequirement())));    }
}