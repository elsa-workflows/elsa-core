using Elsa.Features.Services;
using Elsa.Identity.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extensions for <see cref="IModule"/> that installs the <see cref="IdentityFeature"/> feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Installs & configures the <see cref="IdentityFeature"/> feature.
    /// </summary>
    public static IModule UseIdentity(this IModule module, Action<IdentityFeature> configure)
    {
        module.Configure(configure);
        return module;
    }
    
    /// <summary>
    /// Installs & configures the <see cref="IdentityFeature"/> feature.
    /// </summary>
    public static IModule UseIdentity(this IModule module, string signingKey, string issuer = "http://elsa.api", string audience = "http://elsa.api", TimeSpan? tokenLifetime = default)
    {
        module.UseIdentity(identity => identity.TokenOptions = options =>
        {
            options.Audience = audience;
            options.Issuer = issuer;
            options.AccessTokenLifetime = tokenLifetime ?? TimeSpan.FromHours(1);
            options.SigningKey = signingKey;
        });
        return module;
    }
    
    /// <summary>
    /// Installs & configures the <see cref="IdentityFeature"/> feature.
    /// </summary>
    public static IModule UseDefaultAuthentication(this IModule module, Action<DefaultAuthenticationFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}