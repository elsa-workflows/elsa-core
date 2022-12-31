using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        var identityOptions = identityFeature.IdentityOptions;
        
        Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, identityOptions.ConfigureJwtBearerOptions);
    }
}